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
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class Channel : ICloneable, ICompactSerializable
    {
        int tcpPort, numInitHosts, connectionRetries = 2, connectionRetryInterval = 2;
        int portRange = 1; //muds: default port-range is '1' 
        string initialHosts;

        int joinRetryInterval = 5;
        int joinRetries = 24;



        public Channel() { }
        public Channel(int defaultPortRange) { portRange = defaultPortRange; }

        [ConfigurationAttribute("cluster-port",true,false,"")]
        public int TcpPort
        {
            get { return tcpPort; }
            set { tcpPort = value; }
        }

        [ConfigurationAttribute("port-range")]
        public int PortRange
        {
            get { return portRange; }
            set { portRange = value; }
        }

        [ConfigurationAttribute("connection-retries")]
        public int ConnectionRetries
        {
            get { return connectionRetries; }
            set { connectionRetries = value; }
        }

        [ConfigurationAttribute("connection-retry-interval", "secs")]
        public int ConnectionRetryInterval
        {
            get { return connectionRetryInterval; }
            set { connectionRetryInterval = value; }
        }

        public string InitialHosts
        {
            get { return initialHosts; }
            set { initialHosts = value; }
        }

      
        public int NumInitHosts
        {
            get { return numInitHosts; }
            set { numInitHosts = value; }
        }

        [ConfigurationAttribute("join_retry_count")]
        public int JoinRetries
        {
            get { return joinRetries; }
            set { joinRetries = value; }
        }

        [ConfigurationAttribute("join_retry_timeout")]
        public int JoinRetryInterval
        {
            get { return joinRetryInterval; }
            set { joinRetryInterval = value; }
        }


        #region ICloneable Members

        public object Clone()
        {
            Channel channel = new Channel();
            channel.TcpPort = TcpPort;
            channel.PortRange = PortRange;
            channel.ConnectionRetries = ConnectionRetries;
            channel.ConnectionRetryInterval = ConnectionRetryInterval;
            channel.InitialHosts = InitialHosts != null ? (string) InitialHosts.Clone(): null;
            channel.NumInitHosts = NumInitHosts;
            return channel;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            tcpPort = reader.ReadInt32();
            numInitHosts = reader.ReadInt32();
            connectionRetries = reader.ReadInt32();
            connectionRetryInterval = reader.ReadInt32();
            portRange = reader.ReadInt32();
            initialHosts = reader.ReadObject() as String;

            joinRetryInterval = reader.ReadInt32();
            joinRetries = reader.ReadInt32();

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.Write(tcpPort);
            writer.Write(numInitHosts);
            writer.Write(connectionRetries);
            writer.Write(connectionRetryInterval);
            writer.Write(portRange);
            writer.WriteObject(initialHosts);

            writer.Write(joinRetryInterval);
            writer.Write(joinRetries);

        }
        #endregion
    }
}
