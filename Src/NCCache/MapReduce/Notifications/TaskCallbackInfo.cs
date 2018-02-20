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
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;

namespace Alachisoft.NCache.MapReduce.Notifications
{
    public class TaskCallbackInfo : ICompactSerializable
    {
        private string theClient;
        private short theCallbackId;

        public TaskCallbackInfo() { }

        public TaskCallbackInfo(string client, short callback)
        {
            this.theClient = client;
            this.theCallbackId = callback;
        }
        
        public string Client
        {
            get { return theClient; }
            set { theClient = value; }
        }
        
        public short CallbackId
        {
            get { return theCallbackId; }
            set { theCallbackId = value; }
        }


        public override bool Equals(object obj)
        {
            if (obj is TaskCallbackInfo) {
            TaskCallbackInfo other = (TaskCallbackInfo) ((obj is TaskCallbackInfo) ? obj : null);
            if (!other.Client.Equals(theClient)) {
                return false;
            }
            if (!other.CallbackId.Equals(theCallbackId)) {
                return false;
            }
            return true;
        }
        return false;
        }

        public override string ToString()
        {
            int val = 0;
            return (this.theClient != null ? theClient : "NULL") + ":" + (this.theCallbackId.ToString() != null ? theCallbackId.ToString() : val.ToString());
        }

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.theClient = reader.ReadString();
            this.theCallbackId = reader.ReadInt16();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(this.theClient);
            writer.Write(this.theCallbackId);
        }
    }
}
