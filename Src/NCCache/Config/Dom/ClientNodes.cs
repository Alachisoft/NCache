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
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class ClientNodes : ICloneable,ICompactSerializable
    {
        List<ClientNode> nodesList = new List<ClientNode>();

        
        [ConfigurationSection("client-node")]//Changes for New Dom from node
        public ClientNode[] Nodes
        {
            get
            {
                if (nodesList != null)
                    return nodesList.ToArray();
                return null;
            }
            set
            {
                if (nodesList == null)
                    nodesList = new List<ClientNode>();

                nodesList.Clear();
                if (value != null)
                {
                    nodesList.AddRange(value);
                }
            }
            
        }

        public List<ClientNode> NodesList
        {
            get { return nodesList; }
            set { nodesList = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            ClientNodes clientNodes = new ClientNodes();
            clientNodes.Nodes = Nodes != null ? (ClientNode[])Nodes.Clone() : null;
            return clientNodes;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            Nodes = reader.ReadObject() as ClientNode[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Nodes);
        }

        #endregion
    }
}
