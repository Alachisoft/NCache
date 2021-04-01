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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using System.Collections;

namespace Alachisoft.NCache.Management.ClientConfiguration.Dom
{
    [Serializable]
    [ConfigurationRoot("configuration")]
    public class ClientConfiguration : ICloneable, ICompactSerializable
    {
        private NodeConfiguration _nodeConfig;
        private Dictionary<string, CacheConfiguration> _cacheConfigsMap;

        private string _bindIp;
        
        [ConfigurationSection("ncache-server")]

        public NodeConfiguration NodeConfiguration
        {
            get {return _nodeConfig;}
            set { _nodeConfig = value; }
        }

        [ConfigurationSection("cache")]
        public CacheConfiguration[] CacheConfigurations
        {
            get 
            {
                CacheConfiguration[] configs = null;

                if (_cacheConfigsMap != null)
                {
                    configs = new CacheConfiguration[_cacheConfigsMap.Count];
                    _cacheConfigsMap.Values.CopyTo(configs, 0);
                }

                return configs; 
            }
            set 
            {
                if (_cacheConfigsMap != null && _cacheConfigsMap.Count > 0)
                    _cacheConfigsMap.Clear();
                else
                    _cacheConfigsMap = new Dictionary<string, CacheConfiguration>();

                foreach (CacheConfiguration config in value)
                {
                      _cacheConfigsMap.Add(config.CacheId.ToLower(), config);
                }
            }
        }

        public string BindIp
        {
            get { return _bindIp; }
            set 
            { 
                _bindIp = value;

                if (_cacheConfigsMap != null)
                {
                    foreach (CacheConfiguration config in _cacheConfigsMap.Values)
                    {
                        if (config != null)
                            config.BindIp = value;
                    }
                } 
            }
        }

        public Dictionary<string, CacheConfiguration> CacheConfigurationsMap
        {
            get { return _cacheConfigsMap; }
            set { _cacheConfigsMap = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            ClientConfiguration configuration = new ClientConfiguration();
            configuration.CacheConfigurations = CacheConfigurations != null ? CacheConfigurations.Clone() as CacheConfiguration[] : null;
            configuration._nodeConfig = _nodeConfig != null ? _nodeConfig.Clone() as NodeConfiguration : null;
            configuration.BindIp = _bindIp;

            return configuration;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _nodeConfig = reader.ReadObject() as NodeConfiguration;

            //Needs to be initialized at ever read.
            _cacheConfigsMap = new Dictionary<string, CacheConfiguration>();
            int lenght = reader.ReadInt32();
            for (int i = 0; i < lenght; i++)
            {
                _cacheConfigsMap.Add(reader.ReadObject() as string, reader.ReadObject() as CacheConfiguration);
            }
            _bindIp = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_nodeConfig);
            writer.Write(this._cacheConfigsMap.Count);
            for (IDictionaryEnumerator i = _cacheConfigsMap.GetEnumerator(); i.MoveNext(); )
            {
                writer.WriteObject(i.Key);
                writer.WriteObject(i.Value);
            }
            writer.WriteObject(_bindIp);
        }

        #endregion
    }
}
