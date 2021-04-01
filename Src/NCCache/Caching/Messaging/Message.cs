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
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.Caching.Messaging
{
    public class Message : ISizable, ICloneable, ICompactSerializable
    {
        private static int PrimitiveDTSize = 200;
        private  string _messageId;
        private long _size = -1;
         
        public Message(string messageId)
        {
            _messageId = messageId;
        }

        public string MessageId
        {
            get { return _messageId; }
        }

        public DateTime CreationTime { get; set; }

        public object PayLoad { get; set; }

        public BitSet FlagMap { get; set; }

        public MessageMetaData MessageMetaData { get; set; }

        public bool IsMulticast { get; set; }

        #region ISizable Members

        public int Size
        {
            get { return (int)(PrimitiveDTSize + DataSize); }
        }


        public int InMemorySize
        {
            get
            { return (int)(PrimitiveDTSize + InMemoryDataSize); }
        }

        public long InMemoryDataSize
        {
            get
            {
                int size = 0;
                if (PayLoad != null)
                {
                    if (PayLoad is UserBinaryObject)
                    {
                        //it is to be decided yet
                        size = ((UserBinaryObject)PayLoad).InMemorySize;

                    }
                  
                }
                return size;
            }
        }

        public long DataSize
        {
            get
            {
                if (_size > -1) return _size;
                int size = 0;
                if (PayLoad != null)
                {
                    if (PayLoad is UserBinaryObject)
                    {
                        size = ((UserBinaryObject)PayLoad).Size;
                    }                    
                }

                return size;
            }

            set
            {
                _size = value;
            }
        }

        #endregion

        public object Clone()
        {
            Message messageEntry = new Message(_messageId);
            lock (this)
            {
                messageEntry.CreationTime = this.CreationTime;
                messageEntry.PayLoad = this.PayLoad;
                messageEntry.FlagMap = (BitSet)(this.FlagMap.Clone());
                messageEntry.MessageMetaData = (MessageMetaData)(this.MessageMetaData.Clone());
            }
            return messageEntry;
        }

        public Message CloneWithoutValue()
        {
            Message messageEntry = new Message(_messageId);
            lock (this)
            {
                messageEntry.CreationTime = CreationTime;
                messageEntry.FlagMap = (BitSet)FlagMap.Clone();
                messageEntry.MessageMetaData = (MessageMetaData)MessageMetaData.Clone();
            }
            return messageEntry;
        }


        #region	ICompactSerializable Impl

        public virtual void Deserialize(CompactReader reader)
        {
            lock (this)
            {
                _messageId = reader.ReadObject() as string;
                PayLoad = reader.ReadObject();
                FlagMap =reader.ReadObject() as BitSet;
                _size = reader.ReadInt64();
                CreationTime = reader.ReadDateTime();
                MessageMetaData = MessageMetaData.ReadMetaDataInfo(reader);
                
            }
        }

        public virtual void Serialize(CompactWriter writer)
        {
            lock (this)
            {
                writer.WriteObject(_messageId);
                writer.WriteObject(PayLoad);
                writer.WriteObject(FlagMap);
                writer.Write(_size);
                writer.Write(CreationTime);
                MessageMetaData.WriteMetaDataInfo(writer, MessageMetaData);
            }
        }

        public override int GetHashCode()
        {
            return this.MessageId.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is SubscriptionIdentifier)
            {
                var other = obj as SubscriptionIdentifier;
                if (string.Compare(this.MessageId, other.SubscriptionName, true) == 0 )
                    return true;
            }
            return false;
        }
        #endregion
    }
}
