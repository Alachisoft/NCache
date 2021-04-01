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

using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;
using Alachisoft.NCache.Common.Collections;
using System.Linq;

namespace Alachisoft.NCache.Caching.Messaging
{
    public class EventMessage : EventMessageBase, ICompactSerializable
    {
        public EventCacheEntry Item
        {
            get; set;
        }

        public EventCacheEntry OldItem
        {
            get; set;
        }

        public ItemRemoveReason RemoveReason
        {
            get; set;
        }

     
        public EventMessage(string messageId) : base(messageId)
        {
        }

        public override List<string> GetDestinationClientIds()
        {
            var clients = base.GetDestinationClientIds();
            var clientSet = new HashSet<string>(clients);

            return clients;
        }

        #region	ICompactSerializable Impl

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);

            lock (this)
            {
                Item = EventCacheEntry.ReadItemInfo(reader);
                OldItem = EventCacheEntry.ReadItemInfo(reader);
               
            }
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);

            lock (this)
            {
                EventCacheEntry.WriteItemInfo(writer, Item);
                EventCacheEntry.WriteItemInfo(writer, OldItem);
            }
        }

        #endregion


        public new EventMessage Clone()
        {
            EventMessage eventMessage = new EventMessage(MessageId);
            eventMessage.OldItem = OldItem;
            eventMessage.Item = Item;
            eventMessage.Key = Key;
            eventMessage.IsMulticast = IsMulticast;
            eventMessage.SpecificReciepients = SpecificReciepients;
            eventMessage.RemoveReason = RemoveReason;
            eventMessage.EventID = EventID;
            eventMessage.CallbackInfos = CallbackInfos;
            return eventMessage;

        }
    }
}
