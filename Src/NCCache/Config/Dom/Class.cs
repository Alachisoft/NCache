// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Serialization;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class Class: ICloneable,ICompactSerializable
    {
        string id, name;
        Hashtable attributesTable;
        

        public Class() 
        {
            attributesTable = new Hashtable();
        }

        [ConfigurationAttribute("id")]
        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        [ConfigurationAttribute("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        [ConfigurationSection("query-attributes")]//Changes for New Dom from attrib
        public Attrib[] Attributes
        {
            get 
            { 
                Attrib[] attribs = new Attrib[attributesTable.Count];
                attributesTable.Values.CopyTo(attribs,0);
                return attribs;
            }
            set
            {
                attributesTable.Clear();
                foreach (Attrib attrib in value)
                {
                    attributesTable.Add(attrib.Name, attrib);
                }
            }
        }

        public Hashtable AttributesTable
        {
            get { return attributesTable; }
            set { attributesTable = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Class cls = new Class();
            cls.ID = ID != null ? (string)ID.Clone(): null;
            cls.Name = Name != null ? (string)Name.Clone(): null;
            cls.Attributes = Attributes != null ? (Attrib[])Attributes.Clone(): null;
            return cls;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = reader.ReadObject() as string;
            name = reader.ReadObject() as string;

            attributesTable = reader.ReadObject() as Hashtable;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
  
            writer.WriteObject(attributesTable);
        }

        #endregion
    }
}
