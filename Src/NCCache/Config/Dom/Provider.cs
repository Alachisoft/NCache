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

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Provider: ICloneable,ICompactSerializable
    {
        string providerName, assemblyName, className, _fullProviderName;
        bool isDefaultProvider;
        bool asyncMode;
        Parameter[] parameters;

        public Provider() { }

        [ConfigurationAttribute("provider-name")]
        public string ProviderName
        {
            get { return providerName; }
            set { providerName = value; }
        }

        [ConfigurationAttribute("assembly-name")]
        public string AssemblyName
        {
            get { return assemblyName; }
            set { assemblyName = value; }
        }

        [ConfigurationAttribute("class-name")]
        public string ClassName
        {
            get { return className; }
            set { className = value; }
        }

        [ConfigurationAttribute("full-name")]
        public string FullProviderName
        {
            get { return _fullProviderName; }
            set { _fullProviderName = value; }
        }

        [ConfigurationAttribute("default-provider")]
        public bool IsDefaultProvider
        {
            get { return isDefaultProvider; }
            set { isDefaultProvider = value; }
        }

        [ConfigurationSection("parameters")]//Changes for New Dom from param
        public Parameter[] Parameters
        {
            get { return parameters; }
            set { parameters = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Provider provider = new Provider();
            provider.ProviderName = ProviderName != null ? (string) ProviderName.Clone() : null;
            provider.AssemblyName = AssemblyName != null ? (string) AssemblyName.Clone(): null;
            provider.ClassName = ClassName != null ? (string) ClassName.Clone(): null;
            provider.FullProviderName = FullProviderName != null ? (string)FullProviderName.Clone() : null;
            provider.Parameters = Parameters != null ? Parameters.Clone() as Parameter[]: null;
            return provider;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            providerName = reader.ReadObject() as string;
            assemblyName = reader.ReadObject() as string;
            className = reader.ReadObject() as string;
            _fullProviderName = reader.ReadObject() as string;
            isDefaultProvider = reader.ReadBoolean();
            asyncMode = reader.ReadBoolean();
            parameters = reader.ReadObject() as Parameter[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(providerName);
            writer.WriteObject(assemblyName);
            writer.WriteObject(className);
            writer.WriteObject(_fullProviderName);
            writer.Write(isDefaultProvider);
            writer.Write(asyncMode);
            writer.WriteObject(parameters);
        }

        #endregion
    }
}
