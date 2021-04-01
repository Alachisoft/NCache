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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class PortableClass : ICloneable,ICompactSerializable
    {
        string id;
        string name;
        string assembly;
        string type;

        List<PortableAttribute> portableAttributeList;

        public PortableClass() { }

        [ConfigurationAttribute("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("class-handle-id")]//Changes for New Dom from handle-id
        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        [ConfigurationAttribute("assembly-name")]//Changes for New Dom from assembly
        public string Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        [ConfigurationAttribute("language-platform")]//Changes for New Dom from type
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        [ConfigurationSection("attribute")]
        public PortableAttribute[] PortableAttributes
        {
            get
            {
                if (portableAttributeList != null)
                    return portableAttributeList.ToArray();
                return null;
            }
            set
            {
                if (portableAttributeList == null)
                    portableAttributeList = new List<PortableAttribute>();

                portableAttributeList.Clear();
                if (value != null)
                {
                    portableAttributeList.AddRange(value);
                }
            }
        }
        public List<PortableAttribute> PortableAttributeList
        {
            get { return portableAttributeList; }
            set { portableAttributeList = value; }
        }
        #region ICloneable Members

        public object Clone()
        {
            PortableClass portableClass = new PortableClass();
            portableClass.Name = Name != null ? (string)Name.Clone() : null;
            portableClass.ID = ID != null ? (string)ID.Clone() : null;
            portableClass.Assembly = Assembly != null ? (string)Assembly.Clone() : null;
            portableClass.Type = Type != null ? (string)Type.Clone() : null;
            portableClass.PortableAttributes = PortableAttributes != null ? (PortableAttribute[])PortableAttributes.Clone() : null;
            return portableClass;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
           id = reader.ReadObject() as string;
           name = reader.ReadObject() as string;
           assembly = reader.ReadObject() as string;
           type = reader.ReadObject() as string;
           PortableAttributes = reader.ReadObject() as PortableAttribute[];
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
            writer.WriteObject(assembly);
            writer.WriteObject(type);
            writer.WriteObject(PortableAttributes);
        }

        #endregion
    }
}
