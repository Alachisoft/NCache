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
    public class Attrib: ICloneable,ICompactSerializable
    {
        string id, name, type;

        public Attrib() { }

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

        [ConfigurationAttribute("data-type")]
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        #region ICloneable Members

        public object Clone()
        {
            Attrib attrib = new Attrib();
            attrib.ID = ID != null ? (string) ID.Clone(): null;
            attrib.Name = Name != null ? (string) Name.Clone(): null;
            attrib.Type = Type != null ? (string) Type.Clone(): null;
            return attrib;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = reader.ReadObject() as string;
            name = reader.ReadObject() as string;
            type = reader.ReadObject() as string;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
            writer.WriteObject(type);
        }

        #endregion
    }
}
