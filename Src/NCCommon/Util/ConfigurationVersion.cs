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
using Alachisoft.NCache.Runtime.Serialization;
using System;
using Alachisoft.NCache.Runtime.Serialization.IO;

namespace Alachisoft.NCache.Common.Util
{
    [Serializable]
    public class ConfigurationVersion : ICloneable, ICompactSerializable
    {
        string _id;
        double _configVersion;
        double _depVersion;

        public string ConfigID
        {
            get { return _id; }
            set { _id = value; }
        }

        public double ConfigVersion
        {
            get { return _configVersion; }
            set { _configVersion = value; }
        }
        public double DeploymentVersion
        {
            get { return _depVersion; }
            set { _depVersion = value; }
        }
        public object Clone()
        {
            ConfigurationVersion configurationVersion = new ConfigurationVersion();
            configurationVersion._id = _id;
            configurationVersion._configVersion = _configVersion;
            configurationVersion._depVersion = _depVersion;
            return configurationVersion;

        }

        public void Deserialize(CompactReader reader)
        {
            _id = reader.ReadObject() as string;
            _configVersion = (double)reader.ReadObject();
            _depVersion = (double)reader.ReadObject();
        }

        public void Serialize(CompactWriter writer)
        {
            writer.WriteObject(_id);
            writer.WriteObject(_configVersion);
            writer.WriteObject(_depVersion);
        }
    }
}
