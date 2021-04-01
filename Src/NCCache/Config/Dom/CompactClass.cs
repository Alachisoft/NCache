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
using Newtonsoft.Json;
using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class CompactClass : ICloneable,ICompactSerializable
    {
        private string id;
        private string name;
        private string assembly;
        private string type;
        private bool isGeneric = false;
        private string genericId;
        private string noOfArgs;
        private bool portable = false;
        private Hashtable nonCompactFields;


        public CompactClass() 
        {
            nonCompactFields = new Hashtable();       
        }

        [ConfigurationSection("non-compact-field")]
        public Attrib[] NonCompactFields
        {
            get {

                List<Attrib> fields = new List<Attrib>();            
                IDictionaryEnumerator id = nonCompactFields.GetEnumerator();
                while (id.MoveNext())
                {
                    Attrib temp = id.Value as Attrib;
                    fields.Add(temp);
                }
                
                return fields.ToArray();
            }
            set {

                nonCompactFields.Clear();
                foreach(Attrib field in value)
                {
                    nonCompactFields.Add(field.Name,field);                
                }           
            }        
        }

        public Hashtable NonCompactFieldsTable 
        {
            get { return nonCompactFields; }
            set { nonCompactFields=value; }

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

        [ConfigurationAttribute("assembly")]
        public string Assembly
        {
            get { return assembly; }
            set { assembly = value; }
        }

        [ConfigurationAttribute("type")]
        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        [ConfigurationAttribute("is-generic")]
        public bool IsGeneric
        {
            get { return isGeneric; }
            set { isGeneric = value; }
        }

        [ConfigurationAttribute("generic-id")]
        public string GenericId
        {
            get { return genericId; }
            set { genericId = value; }
        }

        [ConfigurationAttribute("no-of-args")]
        public string NumberOfArgs
        {
            get { return noOfArgs; }
            set { noOfArgs = value; }
        }

        private GenericArgumentType[] _genericArgumentType;
        private List<GenericArgumentType> _genericArgumentTypeList;

        [ConfigurationSection("arguments-type")]
        public GenericArgumentType[] GenericArgumentTypes
        {
            get
            {
                if (_genericArgumentTypeList != null)
                    return _genericArgumentTypeList.ToArray();
                return null;
            }
            set
            {
                if (_genericArgumentTypeList == null)
                    _genericArgumentTypeList = new List<GenericArgumentType>();

                _genericArgumentTypeList.Clear();
                if (value != null)
                {
                    _genericArgumentTypeList.AddRange(value);
                }
            }
        }
        [JsonIgnore]
        public List<GenericArgumentType> GenericArgumentTypeList
        {
            get { return _genericArgumentTypeList; }
            set { _genericArgumentTypeList = value; }
        }
        public bool Portable
        {
            get { return portable; }
        }
        #region ICloneable Members

        public object Clone()
        {
            CompactClass compactClass = new CompactClass();
            compactClass.Name = Name != null ? (string)Name.Clone() : null;
            compactClass.ID = ID != null ? (string)ID.Clone() : null;
            compactClass.Assembly = Assembly != null ? (string) Assembly.Clone() : null;
            compactClass.Type = Type != null ? (string)Type.Clone() : null;
            compactClass.IsGeneric = IsGeneric;
            compactClass.GenericId = GenericId != null ? (string)GenericId.Clone() : null;
            compactClass.NumberOfArgs = NumberOfArgs != null ? (string)NumberOfArgs.Clone() : null;
            compactClass.GenericArgumentTypes = GenericArgumentTypes != null ? (GenericArgumentType[])GenericArgumentTypes.Clone() : null;
            compactClass.NonCompactFields = NonCompactFields != null ? (Attrib[])NonCompactFields.Clone() : null;
            return compactClass;
        }

        #endregion

        #region ICompactSerializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = reader.ReadObject() as string;
            name = reader.ReadObject() as string;
            assembly = reader.ReadObject() as string;
            type = reader.ReadObject() as string;
            isGeneric = reader.ReadBoolean();
            genericId = reader.ReadObject() as string;
            noOfArgs = reader.ReadObject() as string;
            portable = reader.ReadBoolean();
            nonCompactFields = reader.ReadObject() as Hashtable;
            _genericArgumentTypeList = reader.ReadObject() as List<GenericArgumentType>;
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
            writer.WriteObject(assembly);
            writer.WriteObject(type);
            writer.Write(isGeneric);
            writer.WriteObject(genericId);
            writer.WriteObject(noOfArgs);
            writer.Write(portable);
            writer.WriteObject(nonCompactFields);
            writer.WriteObject(_genericArgumentTypeList);
        }

        #endregion
    }
}
