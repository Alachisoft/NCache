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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Runtime.Serialization;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
#if !CLIENT
using Alachisoft.NCache.Caching.Topologies.Clustered;
#endif
using Alachisoft.NCache.Config.Dom;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class ServersNodes : ICloneable,ICompactSerializable
    {
        ArrayList nodesList;

        Dictionary<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo> nodes;

        public ServersNodes()
        {

            nodes = new Dictionary<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo>();
            nodesList = new ArrayList();


        }

        [ConfigurationSection("server-node")]
        public ServerNode[] ServerNodeList
        {
            get
            {
                ServerNode[] serverNode = new ServerNode[nodesList.Count];
                nodesList.CopyTo(serverNode, 0);

                return serverNode;
            }
            set
            {
                nodesList.Clear();
                foreach (ServerNode node in value)
                {
                    nodesList.Add(node);
                }
            }
        }

        public ArrayList NodesList
        {
            get { return nodesList; }
            set { nodesList = value; }
        }

        private string _activeMirrorNode;
        public string ActiveMirrorNode
        {
            get
            {
                if (ServerNodeList != null)
                {
                    foreach (ServerNode server in nodesList)
                    {
                        if (server.IsActiveMirrorNode)
                            return server.IP;
                    }
                }
                return _activeMirrorNode;
            }
            set
            {
                _activeMirrorNode = value;
                if (ServerNodeList != null)
                {
                    foreach (ServerNode server in nodesList)
                    {
                        if (server.IP.Equals(value))
                            server.IsActiveMirrorNode = true;
                    }
                }
            }
        }


        public Dictionary<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo> Nodes
        {
            get { return nodes; }
            set { nodes = value; }
        }


        public List<Alachisoft.NCache.Config.Dom.NodeIdentity> NodeIdentities
        {
            get
            {

                Alachisoft.NCache.Config.Dom.NodeIdentity[] nodeIdentities = new Alachisoft.NCache.Config.Dom.NodeIdentity[nodes.Count];
                nodes.Keys.CopyTo(nodeIdentities, 0);
                return new List<Alachisoft.NCache.Config.Dom.NodeIdentity>(nodeIdentities);

            }
        }

           public bool FindNode(string ip)
        {
            foreach (ServerNode server in nodesList)
            {
                if (server.IP.Equals(ip))
                {
                    return true;
                }
            }
            return false;
        }

        public int NewNodePriority
        {
            get
            {
                int priority = 0;
                if (Nodes == null) return 1;

                foreach (Alachisoft.NCache.Config.Dom.NodeIdentity node in NodeIdentities)
                {
                    if (node.NodePriority > priority) priority = node.NodePriority;
                }

                return priority + 1;
            }
        }

        public void ReAssignPriority(Alachisoft.NCache.Config.Dom.NodeIdentity leavingNode)
        {
            foreach (Alachisoft.NCache.Config.Dom.NodeIdentity node in NodeIdentities)
            {
                if (leavingNode.NodeName == node.NodeName)
                    leavingNode.NodePriority = node.NodePriority;
            }

            foreach (Alachisoft.NCache.Config.Dom.NodeIdentity oldNode in NodeIdentities)
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
            if (ServerNodeList != null)
            {
                foreach (ServerNode sn in nodesList)
                {
                    nodes.Add(new Address(sn.IP, 0));
                }
            }

            return nodes;
        }



        #region ICloneable Members

        public object Clone()
        {
            ServersNodes serverNode = new ServersNodes();
            serverNode.ServerNodeList = ServerNodeList.Clone() as ServerNode[];
            if (nodes != null)
            {

                serverNode.nodes = new Dictionary<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo>();
                foreach (KeyValuePair<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo> pair in nodes)
                {
                    serverNode.nodes.Add(pair.Key, pair.Value);
                }

            }
            return serverNode;
        }

        #endregion



        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
           if (this.nodesList == null) {
            this.nodesList = new ArrayList();
            }
        
            this.nodesList =reader.ReadObject() as ArrayList;
        
            bool nodeExists = reader.ReadBoolean();
            this.nodes = new Dictionary<Alachisoft.NCache.Config.Dom.NodeIdentity, StatusInfo>();
            if (nodeExists) {

                int count = reader.ReadInt32();
                for (int index = 0; index < count; index++) {
                    nodes.Add(reader.ReadObject() as Alachisoft.NCache.Config.Dom.NodeIdentity, reader.ReadObject() as StatusInfo);
                }
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(this.nodesList);
            if (nodes != null && nodes.Count > 0)
            {
                writer.Write(true);
                writer.Write(nodes.Count);
                foreach (Alachisoft.NCache.Config.Dom.NodeIdentity key in nodes.Keys)
                {
                    writer.WriteObject(key);
                    writer.WriteObject(nodes[key]);
                }
            }
            else
            {
                writer.Write(false);
            }
        }
        #endregion
    }
}
