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
    public class CacheTopology : ICloneable,ICompactSerializable
    {
        string topology;
        Cluster clusterSettings;



        public CacheTopology()
        {
            clusterSettings = new Cluster();
        }

        [ConfigurationSection("cluster-settings")]
        public Cluster ClusterSettings
        {
            get { return clusterSettings; }
            set { clusterSettings = value; }
        }

        [ConfigurationAttribute("topology", true, false, "")]
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
                        case "replicated": return "replicated";
                        case "partitioned": return "partitioned";
                        case "partition-replica": return "partitioned-replica";
                        case "mirror": return "mirrored";
                        case "local": return "local-cache";
                        case "client-cache": return "client-cache";
                    }
                }
                return value;
            }
            set { this.topology = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            CacheTopology config = new CacheTopology();
            config.clusterSettings = ClusterSettings != null ? (Cluster)ClusterSettings.Clone() : null;
            config.topology = this.topology != null ? (string)this.topology.Clone() : null;

            return config;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
           topology  = reader.ReadObject() as String;
           clusterSettings = reader.ReadObject() as Cluster;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(topology);
            writer.WriteObject(clusterSettings);
        }
        #endregion
    }
}
