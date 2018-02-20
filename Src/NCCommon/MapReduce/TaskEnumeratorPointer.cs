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
// limitations under the License
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Net;

namespace Alachisoft.NCache.Common.MapReduce
{
    public class TaskEnumeratorPointer : ICompactSerializable
    {
        private string taskId;
        private short callbackId;
        private string clientId;

        public TaskEnumeratorPointer() { }

        private Address clientAddress;
        private int clientPort;

        public int ClientPort
        {
            get { return clientPort; }
            set { clientPort = value; }
        }
        private Address clusterAddress;
        private int clusterPort;

        public int ClusterPort
        {
            get { return clusterPort; }
            set { clusterPort = value; }
        }

        public string TaskId
        {
            get { return taskId; }
            set { taskId = value; }
        }
        public short CallbackId
        {
            get { return callbackId; }
            set { callbackId = value; }
        }
        public string ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }
        public Address ClientAddress
        {
            get { return clientAddress; }
            set { clientAddress = value; }
        }
        public Address ClusterAddress
        {
            get { return clusterAddress; }
            set { clusterAddress = value; }
        }

        public TaskEnumeratorPointer(string clientId, string taskId, short callbackId)
        {
            this.clientId = clientId;
            this.taskId = taskId;
            this.callbackId = callbackId;
        }

        public override bool Equals(object obj)
        {
            if (obj is TaskEnumeratorPointer)
            {
                TaskEnumeratorPointer other = (TaskEnumeratorPointer)((obj is TaskEnumeratorPointer) ? obj : null);
                if (other.ClientId != this.ClientId)
                    return false;
                if (other.CallbackId != this.CallbackId)
                    return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return TaskId.GetHashCode() + CallbackId.GetHashCode() + ClientId.GetHashCode();
        }

        #region ICompactSerialiable Methods

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            this.ClientAddress = (Address)reader.ReadObject();
            this.ClusterAddress = (Address)reader.ReadObject();
            this.ClientId = reader.ReadString();
            this.TaskId = reader.ReadString();
            this.CallbackId = reader.ReadInt16();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this.ClientAddress);
            writer.WriteObject(this.ClusterAddress);
            writer.Write(this.ClientId);
            writer.Write(this.TaskId);
            writer.Write(this.CallbackId);
        }

        #endregion
    }
}
