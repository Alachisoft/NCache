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
using System.Net;
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Runtime.Caching
{
    /// <summary>
    /// This class provides detailed information about cache client.
    /// </summary>
    public class ClientInfo : ICloneable, ICompactSerializable
    {
        /// <summary>
        /// Application's name. 
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// ClientID is a unique id. 
        /// </summary>
        public string ClientID { get; set; }

        /// <summary>
        /// IPAddress of the cache client.
        /// </summary>
        public IPAddress IPAddress { get; set; }

        /// <summary>
        /// Mac Address of cache client
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// Available cores of Cache Client
        /// </summary>
        public int Cores { get; set; }

        /// <summary>
        /// Process ID of the cache client.
        /// </summary>
        public int ProcessID { get; set; }

        /// <summary>
        /// Name of the machine the client is running on.
        /// </summary>
        public string MachineName { get; set; }

        /// <summary>
        /// <see cref="ConnectivityStatus"/>
        /// </summary>
        public ConnectivityStatus Status { get; set; }

        /// <summary>
        /// Version of NCache client. 
        /// </summary>
        public int ClientVersion { get; internal set; }
        public bool IsDotNetCore { get; internal set; }
        public string OperationSystem { get; internal set; }
        public int Memory { get; internal set; }


        /// <summary>
        /// GetLegacyClientId returns the client id in string.
        /// </summary>
        /// <param name="info"><see cref="ClientInfo"/> </param>
        /// <returns>Client id of the client</returns>
        public static string GetLegacyClientID(ClientInfo info)
        {
            return ((info.ClientID != null ? info.ClientID : "")  + ":" + (info.MachineName != null ? info.MachineName : "" )+ ":" + info.ProcessID);
        }
         /// <summary>
         /// TryParseLegacyClientId parse the Client id and get information about the client id in the form of ClientInfo
         /// </summary>
         /// <param name="clientId">Client id of the client</param>
         /// <returns><see cref="ClientInfo"/> </returns>
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
        /// <summary>
        /// Converts Client Info to string , contains client id , Application name ,Process id , machine name and address.
        /// </summary>
        /// <returns>ClientInfo in string form</returns>
        public override string ToString()
        {
            return "Client ID: " + ClientID + Environment.NewLine +
                   "Application Name: " + AppName + Environment.NewLine +
                   "Process ID: " + ProcessID + Environment.NewLine +
                   "Machine Name: " + MachineName + Environment.NewLine +
                   "Status: " + Status + Environment.NewLine +
                   "Address: " + IPAddress + Environment.NewLine +
                   "Client Version: " + ClientVersion;
        }

      
        /// <summary>
        /// Deserializes the Compact reader object passed to it
        /// </summary>
        /// <param name="reader"><see cref="CompactReader"/> </param>
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
            ClientVersion = reader.ReadInt32();
        }
        /// <summary>
        /// Serializes the CompactWriter object
        /// </summary>
        /// <param name="writer"><see cref="CompactWriter"/></param>
        public void Serialize(CompactWriter writer)
        {
            writer.Write(AppName);
            writer.Write(ClientID);
            writer.Write(MachineName);
            writer.Write(ProcessID);
            writer.WriteObject(IPAddress != null ? IPAddress.ToString() : null);
            writer.Write((int)Status);
            writer.Write((int)ClientVersion);
        }
        /// <summary>
        /// Clones the object and returns the newly created clone of the object.
        /// </summary>
        /// <returns>The newly cloned ClientInfo object</returns>
        public object Clone()
        {
            ClientInfo info = new ClientInfo();
            info.IPAddress = IPAddress != null ? IPAddress.Parse(IPAddress.ToString()) : null;
            info.AppName = AppName;
            info.ClientID = ClientID;
            info.MachineName = MachineName;
            info.ProcessID = ProcessID;
            info.Status = Status;
            info.ClientVersion = ClientVersion;
            return info;
        }
    }
}
