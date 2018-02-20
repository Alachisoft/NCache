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
using System.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// This class provides detail information about cache client
    /// </summary>
    public class ClientInfo : ICloneable, ICompactSerializable
    {
        /// <summary>
        /// Application name. 
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// ClientID is a unique id. 
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// IPAddress of the cache client
        /// </summary>
        public IPAddress IPAddress { get; set; }


        /// <summary>
        /// Process ID of the cache client
        /// </summary>
        public int ProcessID { get; set; }

        /// <summary>
        /// Name of the machine the client is running on
        /// </summary>
        public string MachineName { get; set; }

        public ConnectivityStatus Status { get; set; }
        
        public static string GetLegacyClientID(ClientInfo info)
        {
            return ((info.ClientID != null ? info.ClientID : "")  + ":" + (info.MachineName != null ? info.MachineName : "" )+ ":" + info.ProcessID);
        }

        public static ClientInfo TryParseLegacyClientID(string clientId)
        {
            var parameters = clientId.Split(':');
            if (parameters.Length < 3)
                return null;
            ClientInfo info = new ClientInfo();
            info.ClientID = parameters[0];
            info.MachineName = parameters[1];
            int processId;

            if (int.TryParse(parameters[2], out processId))
                info.ProcessID = processId;
            return info;
        }

        public override string ToString()
        {
            return "Client ID: " + ClientID + Environment.NewLine +
                   "Application Name: " + AppName + Environment.NewLine +
                   "Process ID: " + ProcessID + Environment.NewLine +
                   "Machine Name: " + MachineName + Environment.NewLine +
                   "Address: " + IPAddress;
        }


        public void Deserialize(CompactReader reader)
        {
            AppName = reader.ReadString();
            ClientID = reader.ReadString();
            MachineName = reader.ReadString();
            ProcessID = reader.ReadInt32();
            string address = reader.ReadObject() as string;
            if (address != null)
                IPAddress = IPAddress.Parse(address);
            Status = (ConnectivityStatus)reader.ReadInt32();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.Write(AppName);
            writer.Write(ClientID);
            writer.Write(MachineName);
            writer.Write(ProcessID);
            writer.WriteObject(IPAddress != null ? IPAddress.ToString() : null);
            writer.Write((int)Status);
        }

        public object Clone()
        {
            ClientInfo info = new ClientInfo();
            info.IPAddress = IPAddress != null ? IPAddress.Parse(IPAddress.ToString()) : null;
            info.AppName = AppName;
            info.ClientID = ClientID;
            info.MachineName = MachineName;
            info.ProcessID = ProcessID;
            info.Status = Status;
            return info;
        }
    }
}
