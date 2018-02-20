// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
    [Serializable]
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
                        size = ((UserBinaryObject)PayLoad).InMemorySize;
                    }
                    else if (PayLoad is CallbackEntry)
                    {
                        CallbackEntry entry = (CallbackEntry)PayLoad;
                        if (entry.Value != null)
                        {
                            if (entry.Value is UserBinaryObject)
                                size = ((UserBinaryObject)(entry.Value)).InMemorySize;
                            else if (entry.Value is byte[])
                                size = ((byte[])entry.Value).Length;
                        }
                        size += entry.InMemorySize;
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
                    else if (PayLoad is CallbackEntry)
                    {
                        CallbackEntry entry = (CallbackEntry)PayLoad;
                        if (entry.Value != null && entry.Value is UserBinaryObject)
                            size = ((UserBinaryObject)(entry.Value)).Size;
                    }
                }

                return size;
            }

            set
            {
                _size = value;
            }
        }

        internal IEnumerable<ClientSubscriptionManager> ClientSubscriptions { get; set; }

        #endregion

        public object Clone()
        {
            Message messageEntry = new Message(_messageId);
            lock (this)
            {
                messageEntry.CreationTime = CreationTime;
                messageEntry.PayLoad = PayLoad;
                messageEntry.FlagMap = (BitSet)FlagMap.Clone();
                messageEntry.MessageMetaData = (MessageMetaData)MessageMetaData.Clone();
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

        public void Deserialize(CompactReader reader)
        {
            lock (this)
            {
                _messageId = reader.ReadObject() as string;
                PayLoad = reader.ReadObject();
                FlagMap = new BitSet(reader.ReadByte());
                _size = reader.ReadInt64();
                CreationTime = reader.ReadDateTime();
                MessageMetaData = MessageMetaData.ReadMetaDataInfo(reader);
                
            }
        }

        public void Serialize(CompactWriter writer)
        {
            lock (this)
            {
                writer.WriteObject(_messageId);
                writer.WriteObject(PayLoad);
                writer.Write(FlagMap.Data);
                writer.Write(_size);
                writer.Write(CreationTime);
                MessageMetaData.WriteMetaDataInfo(writer, MessageMetaData);
            }
        }

        #endregion

    }
}
