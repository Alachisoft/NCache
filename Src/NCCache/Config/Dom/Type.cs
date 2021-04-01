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
using Alachisoft.NCache.Config.Dom;
using System.Collections.Generic;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Type: ICloneable,ICompactSerializable
    {
        string id, name;
        bool portable = false;
        PortableClass[] portableClasses;
        List<PortableClass> portableClassList;
        AttributeListUnion attrbiuteList;

        public Type() { }

        [ConfigurationAttribute("id")]
        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        [ConfigurationAttribute("handle")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationAttribute("portable")]
        public bool Portable
        {
            get { return portable; }
            set { portable = value; }
        }

        [ConfigurationSection("all-attribute-list")]//Changes for New Dom from attribute-list
        public AttributeListUnion AttributeList
        {
            get { return attrbiuteList; }
            set { attrbiuteList = value; }
        }

        [ConfigurationSection("sharing-class")]//Changes for New Dom from class
        public PortableClass[] PortableClasses
        {
            get
            {
                if (portableClassList != null)
                    return portableClassList.ToArray();
                return null;
            }
            set
            {
                if (portableClassList == null)
                    portableClassList = new List<PortableClass>();

                portableClassList.Clear();
                if (value != null)
                {
                    portableClassList.AddRange(value);
                }
            }
        }

        public List<PortableClass> PortableClassList
        {
            get { return portableClassList; }
            set { portableClassList = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Type type = new Type();
            type.ID = ID != null ? (string)ID.Clone() : null;
            type.Name = Name != null ? (string)Name.Clone() : null;
            type.Portable = this.Portable;
            type.PortableClasses = PortableClasses != null ? (PortableClass[])PortableClasses.Clone() : null;
            type.AttributeList = AttributeList != null ? (AttributeListUnion)AttributeList.Clone() : null;
            return type;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = reader.ReadObject() as string;
            name = reader.ReadObject() as string;
            portable = reader.ReadBoolean();
            portableClasses = reader.ReadObject() as PortableClass[];
            PortableClasses = reader.ReadObject() as PortableClass[];
            attrbiuteList = reader.ReadObject() as AttributeListUnion;    
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
            writer.Write(portable);
            writer.WriteObject(portableClasses);
            writer.WriteObject(PortableClasses);
            writer.WriteObject(attrbiuteList);
        }

        #endregion
    }
}
