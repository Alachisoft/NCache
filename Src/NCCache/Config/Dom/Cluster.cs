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
using System.Collections.Generic;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using System.Text.RegularExpressions;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Cluster: ICloneable,ICompactSerializable
    {
        string topology;
        string activeMirrorNode = null;
        int opTimeout = 60;
        int statsRepInterval;

        Channel channel;

        Dictionary<NodeIdentity, StatusInfo> nodes;


        public Cluster() 
        {
            channel = new Channel();

            nodes = new Dictionary<NodeIdentity, StatusInfo>();

        }
         
        [ConfigurationAttribute("topology")]
        public string CacheType
        {
            get { return this.topology; }
            set { this.topology = value; }
        }

        /// <summary>
        /// Get the topology type
        /// </summary>
        public string Topology
        {
            get
            {
                string value = this.topology;
                if (value != null)
                {
                    value = value.ToLower();
                    switch (value)
                    {
                        case "replicated": return "replicated-server";
                        case "partitioned": return "partitioned-server";
                    }
                }
                return value;
            }
            set { this.topology = value; }
        }

        [ConfigurationAttribute("op-timeout", "sec")]
        public int OpTimeout
        {
            get { return opTimeout; }
            set { opTimeout = value; }
        }

        [ConfigurationAttribute("stats-repl-interval", "sec")]
        public int StatsRepInterval
        {
            get { return statsRepInterval; }
            set { statsRepInterval = value; }
        }

        [ConfigurationSection("channel")]
        public Channel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        public Dictionary<NodeIdentity, StatusInfo> Nodes
        {
            get { return nodes; }
            set { nodes = value; }
        }


        public List<NodeIdentity> NodeIdentities
        {
            get
            {

                NodeIdentity[] nodeIdentities = new NodeIdentity[nodes.Count];
                nodes.Keys.CopyTo(nodeIdentities, 0);
                return new List<NodeIdentity>(nodeIdentities);

            }
        }

        [ConfigurationAttribute("active-mirror-node")]
        public string ActiveMirrorNode
        {
            get { return activeMirrorNode; }
            set { activeMirrorNode = value; }
        }

        public int NewNodePriority
        {
            get
            {
                int priority = 0;
                if (Nodes == null) return 1;

                foreach (NodeIdentity node in NodeIdentities)
                {
                    if (node.NodePriority > priority) priority = node.NodePriority;
                }

                return priority + 1;
            }
        }

        public void ReAssignPriority(NodeIdentity leavingNode)
        {
            foreach (NodeIdentity node in NodeIdentities)
            {
                if (leavingNode.NodeName == node.NodeName) 
                    leavingNode.NodePriority = node.NodePriority;
            }

            foreach (NodeIdentity oldNode in NodeIdentities)
            {
                if (oldNode.NodePriority > leavingNode.NodePriority)
                    oldNode.NodePriority = oldNode.NodePriority - 1; 
            }
        }

        /// <summary>
        /// Gets the list of all configured servers in the cache based on
        /// initial-host list.
        /// </summary>
        /// <returns></returns>
        public List<Address> GetAllConfiguredNodes()
        {
            List<Address> nodes = new List<Address>();

            if (Channel != null)
            {
                string[] splitted = Channel.InitialHosts.Split(new char[] { ',' });

                string nameOrIP = null;
                int port;
                
                foreach (string hostString in splitted)
                {
                    int firstBrace = hostString.IndexOf("[");
                    int lastBrace = hostString.IndexOf("]");
                    if (firstBrace > 0 && lastBrace > firstBrace)
                    {
                        nameOrIP = hostString.Substring(0, firstBrace);
                        port = Convert.ToInt32(hostString.Substring(firstBrace + 1, lastBrace - firstBrace - 1));
                        nodes.Add(new Address(nameOrIP,port));
                    }
                }
            }

            return nodes;
        }

        #region ICloneable Members

        public object Clone()
        {
            Cluster cluster = new Cluster();
            cluster.topology = this.topology != null ? (string) this.topology.Clone(): null;
            cluster.OpTimeout = OpTimeout;
            cluster.StatsRepInterval = StatsRepInterval;
            cluster.activeMirrorNode = activeMirrorNode;

            if (nodes != null)
            {

                cluster.nodes = new Dictionary<NodeIdentity, StatusInfo>();
                foreach (KeyValuePair<NodeIdentity, StatusInfo> pair in nodes)
                {
                    cluster.nodes.Add(pair.Key, pair.Value);
                }

            }

            cluster.Channel = Channel != null ? (Channel) Channel.Clone(): null;
            return cluster;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
           
            topology = reader.ReadObject() as string;
            activeMirrorNode = reader.ReadObject() as string;
            opTimeout = reader.ReadInt32();
            statsRepInterval = reader.ReadInt32();

            channel = reader.ReadObject() as Channel;

            bool nodeExists = reader.ReadBoolean();
            if (nodeExists)
            {
                nodes = new Dictionary<NodeIdentity, StatusInfo>();
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    nodes.Add(reader.ReadObject() as NodeIdentity , reader.ReadObject() as StatusInfo);
                }
            }

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
           
            writer.WriteObject(topology);
            writer.WriteObject(activeMirrorNode);
            writer.Write(opTimeout);
            writer.Write(statsRepInterval);

            writer.WriteObject(channel);

            if (nodes != null)
            {
                writer.Write(true);
                writer.Write(nodes.Count);
                IDictionaryEnumerator ide = nodes.GetEnumerator();

                while (ide.MoveNext())
                {
                    writer.WriteObject(ide.Key);
                    writer.WriteObject(ide.Value);
                }
            }
            else
                writer.Write(false);
            
        }

        #endregion      
    }
}
