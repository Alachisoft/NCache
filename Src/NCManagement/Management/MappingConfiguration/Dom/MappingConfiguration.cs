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
using Alachisoft.NCache.Runtime.Serialization;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config;

namespace Alachisoft.NCache.Management.MappingConfiguration.Dom
{
    [Serializable]
    [ConfigurationRoot("configuration")]
    public class MappingConfiguration : ICloneable, ICompactSerializable
    {
        private ServerMapping _managementMapping = new ServerMapping();
        private ServerMapping _clientMapping = new ServerMapping();

        [ConfigurationSection("management-end-point")]
        public ServerMapping ManagementIPMapping
        {
            get
            {
                return _managementMapping;
            }
            set
            {
                _managementMapping = value;
            }
        }

        [ConfigurationSection("server-end-point")]
        public ServerMapping ClientIPMapping
        {
            get
            {

                return _clientMapping;
            }
            set
            {
                _clientMapping = value;
            }
        }

        #region ICloneable Members

        public object Clone()
        {
            MappingConfiguration configuration = new MappingConfiguration();
            configuration.ManagementIPMapping = (ServerMapping)ManagementIPMapping.Clone();
            configuration.ClientIPMapping = (ServerMapping)ClientIPMapping.Clone();
            return configuration;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _managementMapping = reader.ReadObject() as ServerMapping;
            _clientMapping = reader.ReadObject() as ServerMapping;  
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_managementMapping);
            writer.WriteObject(_clientMapping);
        }

        #endregion
    }
}
