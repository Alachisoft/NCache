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
using System.Collections;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;

using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
    public abstract class EventMessageBase : MultiCastMessage, ICloneable, ICompactSerializable
    {
        #region Properties

        public string Key
        {
            get; set;
        }

        public EventId EventID
        {
            get; set;
        }

        public ArrayList CallbackInfos
        {
            get; set;
        }

        public string TaskFailureReason
        {
            get; set;
        }
        

        #endregion

        public EventMessageBase(string messageId) : base(messageId)
        {
        }

        public virtual List<string> GetDestinationClientIds()
        {
            List<string> clients = new List<string>();

            if (CallbackInfos != null)
            {
                foreach (CallbackInfo callbackInfo in CallbackInfos)
                {
                    clients.Add(callbackInfo.Client);
                }
            }
            return clients;
        }

        #region	ICompactSerializable Impl

        public override void Deserialize(CompactReader reader)
        {
            base.Deserialize(reader);

            lock (this)
            {
                Key = reader.ReadObject() as string;
                CallbackInfos = (ArrayList)reader.ReadObject();
                EventID = EventId.ReadEventIdInfo(reader);
            }
        }

        public override void Serialize(CompactWriter writer)
        {
            base.Serialize(writer);

            lock (this)
            {
                writer.WriteObject(Key);
                writer.WriteObject(CallbackInfos);
                EventId.WriteEventIdInfo(writer, EventID);
            }
        }

        #endregion
    }
}
