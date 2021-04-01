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
using Alachisoft.NCache.Runtime.Caching;
using System;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Messaging;

namespace Alachisoft.NCache.Common
{
    public class MessageMetaData : ICloneable, ICompactSerializable, ISizable
    {
        private string _messageId;
        private HashSet<string> _recepientList;
        private HashSet<ISubscription> _subscriptons;
        private HashSet<SubscriptionIdentifier> _durableSubscriptions;
        private List<SubscriptionIdentifier> _recipientIdentifierList;

        private object _mutex = new object();

        public MessageMetaData(string messageId)
        {
            _messageId = messageId;
            _recepientList = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            _subscriptons = new HashSet<ISubscription>();
            _durableSubscriptions = new HashSet<SubscriptionIdentifier>();
            _recipientIdentifierList = new List<SubscriptionIdentifier>();
        }

 
        public MessageMetaData()
        {
            _recepientList = new HashSet<string>();
        }

        public HashSet<string> RecepientList
        {
            get
            {
                lock (_mutex) { return _recepientList; }
            }
        }

        public string MessageId
        {
            get { return _messageId; }
        }

        public HashSet<ISubscription> Subscriptions
        {
            get { lock (_mutex) { return _subscriptons; }  }
        }
        public bool IsRemovable
        {
            get
            {
                //lock (_recepientList)
                lock(_mutex)
                {
                    return _recepientList.Count == 0;
                }
            }
        }
        public void AddDurableSubscription(SubscriptionIdentifier subIdentifier)
        {
            lock (_mutex)
            {
                if (!_durableSubscriptions.Contains(subIdentifier))
                    _durableSubscriptions.Add(subIdentifier);
            }
           
        }

        public HashSet<SubscriptionIdentifier> GetDurableSubscriptions()
        {
            HashSet<SubscriptionIdentifier> subIdList = new HashSet<SubscriptionIdentifier>();
            lock (_mutex)
            {
                foreach (var item in _durableSubscriptions)
                {
                    subIdList.Add((SubscriptionIdentifier)item.Clone());
                }
                return subIdList;
            }
        }

        public void AddToRecepientList(SubscriptionIdentifier subscriptionIdentifier)
        {
            lock (_mutex)
            {
                if (!_recipientIdentifierList.Contains(subscriptionIdentifier))
                    _recipientIdentifierList.Add(subscriptionIdentifier);
            }
           
        }

        public void RemoveFromReciepientList(SubscriptionIdentifier subscriptionIdentifier)
        {
            lock (_mutex)
            {
                if (_recipientIdentifierList != null)
                {
                    _recipientIdentifierList.Remove(subscriptionIdentifier);
                }
                if (_durableSubscriptions != null)
                {
                    _durableSubscriptions.Remove(subscriptionIdentifier);
                }
            }
        }
        public long ExpirationTime { get; set; }

        public double TimeToLive { get; set; }

        public DateTime? AssigmentTime { get; set; }

        public string TopicName { get; set; }

        public DeliveryOption DeliveryOption { get; set; }

        public bool IsAssigned { get; set; }

        public bool IsNotify { get; set; }

        internal MessgeFailureReason MessgeFailureReason { get; set; }

        public SubscriptionType SubscriptionType { get; set; }

        public int Size
        {
            get
            {
                return MemoryUtil.NetLongSize +
                    2 * MemoryUtil.NetIntSize +
                    MemoryUtil.NetDateTimeSize +
                    3 * MemoryUtil.NetByteSize +
                    3 * MemoryUtil.NetEnumSize +
                    MemoryUtil.GetStringSize(TopicName) +
                    MemoryUtil.GetStringSize(MessageId);
            }
        }

        public int InMemorySize
        {
            get
            {
                return MemoryUtil.NetLongSize +
                    2 * MemoryUtil.NetIntSize +
                    MemoryUtil.NetDateTimeSize +
                    3 * MemoryUtil.NetByteSize +
                    3 * MemoryUtil.NetEnumSize +
                    MemoryUtil.GetStringSize(TopicName) +
                    MemoryUtil.GetStringSize(MessageId);
            }
        }

        public bool Delivered
        {
            get
            {
                lock (_mutex)
                {
                    if (IsAssigned)
                    {
                        return _subscriptons.Count == 0 && _recepientList.Count == 0 && _durableSubscriptions.Count == 0;
                    }
                    return false;
                }
            }
        }
        public DateTime? AbsoluteExpiratoinTime { get; private set; }
        public bool DeliveryFailed { get; set; }
        public bool EverAcknowledged { get; set; }
        public bool HasSubscriptions { get { return _subscriptons.Count > 0; } }

        public void RemoveRecepient(string recepient)
        {
            lock (_mutex)
            {
                _recepientList.Remove(recepient);
            }
        }

        public void AddRecepient(string recepient)
        {
            lock (_mutex)
            {
                _recepientList.Add(recepient);
            }
        }
        public List<SubscriptionIdentifier> SubscriptionIdentifierList
        {
            get
            {
                return _recipientIdentifierList;
            }
        }
        public void RegisterSubscription(ISubscription subscription)
        {
            lock(_mutex)
            {
                if (!_subscriptons.Contains(subscription))
                {
                    if (AssigmentTime == null) AssigmentTime = DateTime.UtcNow;
                    IsAssigned = true;
                    _subscriptons.Add(subscription);
                }
            }
        }

        public void UnregisterSubscription(ISubscription subscription)
        {
            lock (_mutex)
            {
                if (_subscriptons.Contains(subscription))
                {
                    _subscriptons.Remove(subscription);
                }
            }
        }

           public object Clone()
        {
            lock (_mutex)
            {
                MessageMetaData metaData = new MessageMetaData(MessageId);
                metaData.TimeToLive = TimeToLive;
                metaData.AssigmentTime = AssigmentTime;
                metaData.TopicName = TopicName;
                metaData.DeliveryOption = DeliveryOption;
                metaData.MessgeFailureReason = MessgeFailureReason;
                metaData.SubscriptionType = SubscriptionType;
                metaData.IsAssigned = IsAssigned;
                metaData.IsNotify = IsNotify;
                metaData.AbsoluteExpiratoinTime = AbsoluteExpiratoinTime;
                metaData.EverAcknowledged = EverAcknowledged;
                metaData.ExpirationTime = ExpirationTime;
                metaData._recepientList = new HashSet<string>(RecepientList, StringComparer.InvariantCultureIgnoreCase);
                metaData._subscriptons = new HashSet<ISubscription>(_subscriptons);
                metaData._durableSubscriptions = new HashSet<SubscriptionIdentifier>(_durableSubscriptions);
                metaData._recipientIdentifierList = new List<SubscriptionIdentifier>(_recipientIdentifierList);
                return metaData;
            }
          
        }

        public DateTime? InitializeExpiration()
        {
            lock (_mutex)
            {
                AbsoluteExpiratoinTime = DateTime.UtcNow.Add(new TimeSpan(ExpirationTime));
                return AbsoluteExpiratoinTime;
            }
           
        }

        public void OnMessageRemoved()
        {
            RevokeSubscriptions();
        }

        public void RevokeSubscriptions()
        {
            lock (_mutex)
            {
                foreach (ISubscription clientSubscription in _subscriptons)
                {
                    clientSubscription.OnMessageRemoved(this.MessageId);
                }
                _subscriptons.Clear();
                _recepientList.Clear();
            }

        }

        public void RevokeSubscriptions(ISubscription subscription)
        {
            if(_subscriptons.Contains(subscription))
            {
                subscription.OnMessageRemoved(this.MessageId);
            }
        }

        public static MessageMetaData ReadMetaDataInfo(CompactReader reader)
        {
            bool flag = reader.ReadBoolean();

            if (flag)
            {
                MessageMetaData messageMetaData = new MessageMetaData();
                messageMetaData.Deserialize(reader);
                return messageMetaData;
            }
            return null;
        }

        public static void WriteMetaDataInfo(CompactWriter writer, MessageMetaData messageMetaData)
        {
            if (messageMetaData == null)
            {
                writer.Write(false);
                return;
            }
            else
            {
                writer.Write(true);
                messageMetaData.Serialize(writer);
            }
        }

        public void Deserialize(CompactReader reader)
        {
            _mutex = new object();
            _messageId = reader.ReadObject() as string;
            TimeToLive = reader.ReadDouble();
            AssigmentTime = reader.ReadObject() as DateTime?;
            TopicName = reader.ReadObject() as string;
            DeliveryOption = (DeliveryOption)reader.ReadInt32();
            MessgeFailureReason = (MessgeFailureReason)reader.ReadInt32();
            SubscriptionType = (SubscriptionType)reader.ReadInt32();
            IsAssigned = reader.ReadBoolean();
            IsNotify = reader.ReadBoolean();
            AbsoluteExpiratoinTime = reader.ReadObject() as DateTime?;
            DeliveryFailed = reader.ReadBoolean();
            EverAcknowledged = reader.ReadBoolean();
            ExpirationTime = reader.ReadInt64();
            _recepientList= SerializationUtility.DeserializeHashSet<string>(reader);
            _subscriptons = new HashSet<ISubscription>();
            _durableSubscriptions = SerializationUtility.DeserializeHashSet<SubscriptionIdentifier>(reader);
            _recipientIdentifierList = SerializationUtility.DeserializeList<SubscriptionIdentifier>(reader);

        }

        public void Serialize(CompactWriter writer)
        {
            lock (_mutex)
            {
                writer.WriteObject(_messageId);
                writer.Write(TimeToLive);
                writer.WriteObject(AssigmentTime);
                writer.WriteObject(TopicName);
                writer.Write((int)DeliveryOption);
                writer.Write((int)MessgeFailureReason);
                writer.Write((int)SubscriptionType);
                writer.Write(IsAssigned);
                writer.Write(IsNotify);
                writer.WriteObject(AbsoluteExpiratoinTime);
                writer.Write(DeliveryFailed);
                writer.Write(EverAcknowledged);
                writer.Write(ExpirationTime);
                SerializationUtility.SerializeHashSet(_recepientList, writer);
                SerializationUtility.SerializeHashSet(_durableSubscriptions, writer);
                SerializationUtility.SerializeList(_recipientIdentifierList, writer);
            }
     
        }
    }
}
