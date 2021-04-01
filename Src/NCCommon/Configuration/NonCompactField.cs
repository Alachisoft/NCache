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
namespace Alachisoft.NCache.Common.Configuration
{
    [Serializable]
  public class NonCompactField : ICloneable, Runtime.Serialization.ICompactSerializable
        {
            string id, name, type;

            public NonCompactField() { }

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
                NonCompactField nonCompactField = new NonCompactField();
                nonCompactField.ID = ID != null ? (string)ID.Clone() : null;
                nonCompactField.Name = Name != null ? (string)Name.Clone() : null;
                nonCompactField.Type = Type != null ? (string)Type.Clone() : null;
                return nonCompactField;
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

