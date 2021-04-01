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
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Caching.Topologies.Clustered.Operations.Messaging
{
    class GetTransferrableMessageOperation:ICompactSerializable
    {
        private string _topic;
        private string _message;
        

        public string Topic
        {
            get { return _topic; }
            set { _topic = value; }
        }

        public string Message
        {
            get { return _message; }
            set { _message = value; }
        }

        public GetTransferrableMessageOperation(string topic, string message)
        {
            _topic = topic;
            _message = message;
       }

        public void Deserialize(CompactReader reader)
        {
            _topic = reader.ReadObject() as string;
            _message = reader.ReadObject() as string;
         }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_topic);
            writer.WriteObject(_message);
        }
    }
}
