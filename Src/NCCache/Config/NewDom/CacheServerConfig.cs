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
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Config.NewDom
{
    [Serializable]
    [ConfigurationRoot("cache-config")]
    public class CacheServerConfig : ICloneable, ICompactSerializable
    {
        CacheServerConfigSetting cacheSettings;
        CacheDeployment cacheDeployment;
        bool cacheIsRunning = false;
        bool cacheIsRegistered = false;
        bool licenseIsExpired = false;
        double configVersion;
        string configID;
        string name;
        string _alias = string.Empty;
        public CacheServerConfig()
        {
            cacheSettings = new CacheServerConfigSetting();
        }

        [ConfigurationSection("cache-settings", true, false)]
        public CacheServerConfigSetting CacheSettings
        {
            get { return cacheSettings; }
            set { cacheSettings = value; }
        }

        [ConfigurationSection("cache-deployment")]
        public CacheDeployment CacheDeployment
        {
            get { return cacheDeployment; }
            set { cacheDeployment = value; }
        }

        [ConfigurationAttribute("cache-name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("alias")]
        public string Alias
        {
            get { return _alias; }
            set { _alias = value; }
        }

        [ConfigurationAttribute("config-id")]
        public string ConfigID
        {
            get { return configID; }
            set { configID = value; }
        }

        [ConfigurationAttribute("config-version")]
        public double ConfigVersion
        {
            get { return configVersion; }
            set { configVersion = value; }
        }

        public bool IsRegistered
        {
            get { return cacheIsRegistered; }
            set { cacheIsRegistered = value; }
        }

        public bool IsRunning
        {
            get
            {
                bool isRunning = cacheIsRunning;

                if (this.CacheSettings.CacheType == Alachisoft.NCache.Common.Enum.CacheTopologyType.ClusteredCache)
                {

                    foreach (StatusInfo cacheStatus in cacheDeployment.Servers.Nodes.Values)
                    {
                        if (cacheStatus.Status == CacheStatus.Running)
                        {
                            isRunning = true;
                            break;
                        }
                    }

                }

                return isRunning;
            }

            set
            {
                if (this.cacheSettings.CacheType == "local-cache" || this.cacheSettings.CacheType == "client-cache")
                    cacheIsRunning = value;
            }
        }

        public bool IsExpired
        {
            get { return licenseIsExpired; }
            set { licenseIsExpired = value; }
        }


        public string UniqueId
        {
            get
            {
                if (string.IsNullOrEmpty(_alias))
                    return name;
                return name + "[" + _alias + "]";
            }
       
        }


        #region ICloneable Members

        public object Clone()
        {
            CacheServerConfig config = new CacheServerConfig();
            config.cacheSettings = CacheSettings != null ? (CacheServerConfigSetting)CacheSettings.Clone() : null;
            config.cacheDeployment = CacheDeployment != null ? (CacheDeployment)CacheDeployment.Clone() : null;
            config.ConfigID = ConfigID;
            config.configVersion = configVersion;
            config.IsRegistered = this.IsRegistered;
            config.IsRunning = this.IsRunning;
            config.licenseIsExpired = this.licenseIsExpired;
            config.name = this.name;
            config._alias = this._alias;

            return config;
        }

        #endregion

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            name = reader.ReadObject() as string;
            _alias = reader.ReadObject() as string;
            this.cacheSettings = reader.ReadObject() as CacheServerConfigSetting;
            this.cacheDeployment = reader.ReadObject() as CacheDeployment;
            this.configID = reader.ReadString();
            this.configVersion = reader.ReadDouble();
            cacheIsRunning = reader.ReadBoolean();
            cacheIsRegistered = reader.ReadBoolean();
            licenseIsExpired = reader.ReadBoolean();
            
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(Name);
            writer.WriteObject(_alias);
            writer.WriteObject(cacheSettings);
            writer.WriteObject(this.cacheDeployment);
            writer.Write(configID);
            writer.Write(configVersion);
            writer.Write(cacheIsRunning);
            writer.Write(cacheIsRegistered);
            writer.Write(licenseIsExpired);
        }
        #endregion
    }
}
