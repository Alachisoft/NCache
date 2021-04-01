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
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using System.Web;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Parameter : ICloneable, ICompactSerializable
    {
        string name, paramValue;

        public Parameter() { }

        [ConfigurationAttribute("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("value")]
        public string ParamValue
        {
            get {
                return paramValue;
                }
            set { paramValue = value; }
        } 

        #region ICloneable Members

        public object Clone()
        {
            Parameter parameters = new Parameter();
            parameters.Name = Name != null ? (string)Name.Clone() : null;
            parameters.ParamValue = ParamValue != null ? (string)ParamValue.Clone() : null;
            return parameters;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            name = reader.ReadObject() as string;
            paramValue = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(name);
            writer.WriteObject(paramValue);
        }

        #endregion
    }
}