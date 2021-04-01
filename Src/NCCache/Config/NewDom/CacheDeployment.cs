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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    public class CacheDeployment : ICloneable,ICompactSerializable
    {
        Alachisoft.NCache.Config.Dom.ClientNodes clientNodes;
        ServersNodes serverNodes;
        double depVersion = 0;

        public CacheDeployment()
        {
            serverNodes = new ServersNodes();
        }

        [ConfigurationAttribute("deployment-version")]
        public double DeploymentVersion
        {
            get { return depVersion; }
            set { depVersion = value; }
        }


        [ConfigurationSection("client-nodes")]
        public Alachisoft.NCache.Config.Dom.ClientNodes ClientNodes
        {
            get { return clientNodes; }
            set { clientNodes = value; }
        }

        [ConfigurationSection("servers")]
        public ServersNodes Servers
        {
            get { return serverNodes; }
            set { serverNodes = value; }
        }


        #region ICloneable Members

        public object Clone()
        {
            CacheDeployment config = new CacheDeployment();
            config.clientNodes = clientNodes != null ? clientNodes.Clone() as Alachisoft.NCache.Config.Dom.ClientNodes : null;
            config.serverNodes = serverNodes != null ? serverNodes.Clone() as ServersNodes : null;
            config.DeploymentVersion = depVersion;
            return config;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            clientNodes = (Alachisoft.NCache.Config.Dom.ClientNodes)reader.ReadObject();
            serverNodes = (ServersNodes)reader.ReadObject();
            depVersion = (double)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(clientNodes);
            writer.WriteObject(serverNodes);
            writer.WriteObject(depVersion);
        }
        #endregion
    }
}
