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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Messaging
{
    class TransferrableMessage:ICompactSerializable
    {
        private Message _message;
        private IList<string> _subscribedClientIds = new List<string>(); 

        public Message Message { get { return _message; } }

        public IList<string> SubscribedClients { get { return _subscribedClientIds; } }

        public TransferrableMessage(Message message)
        {
            _message = message;
        }

        public void AddSubscribedClientId(string clientId)
        {
            _subscribedClientIds.Add(clientId);
        }

        public void Deserialize(CompactReader reader)
        {
            _message = reader.ReadObject() as Message;
            _subscribedClientIds = SerializationUtility.DeserializeList<string>(reader);
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_message);
            SerializationUtility.SerializeList<string>(_subscribedClientIds as List<string>, writer);
        }

        
    }
}