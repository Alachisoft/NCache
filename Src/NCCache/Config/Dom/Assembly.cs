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
using System.Collections.Generic;
using Runtime = Alachisoft.NCache.Runtime;

namespace Alachisoft.NCache.Config.Dom
{    
    [Serializable]
    public class Assembly : ICloneable, Runtime.Serialization.ICompactSerializable
    {
        string id, name;
        IDictionary<string, Type> typesMap;

        public Assembly() { }

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

        [ConfigurationSection("type")]
        public Type[] Types
        {
            get
            {
                if (typesMap == null)
                    return null;

                Type[] types = new Type[typesMap.Count];
                typesMap.Values.CopyTo(types, 0);
                return types;
            }
            set 
            {
                if (typesMap == null)
                    typesMap = new Dictionary<string, Type>();

                typesMap.Clear();
                foreach (Type type in value)
                {
                    typesMap.Add(type.Name, type);
                }
            }
        }

        public IDictionary<string, Type> TypesMap
        {
            get { return typesMap; }
            set { typesMap = value; }
        }

        public override int GetHashCode()
        {
            return name.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Assembly)
            {
                return ((Assembly)obj).name.ToLower() == name.ToLower();
            }

            return false;
        }

        #region ICloneable Members

        public object Clone()
        {
            Assembly assembly = new Assembly();
            assembly.ID = ID != null ? (string) ID.Clone(): null;
            assembly.Name =  Name != null ? (string)Name.Clone(): null;
            assembly.Types = Types != null ? (Type[]) Types.Clone(): null;
            return assembly;
        }

        #endregion

        public static Assembly GetExecutingAssembly()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public object GetManifestResourceStream(string p)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        #region ICompactSerializable Members
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = (string)reader.ReadObject();
            name = (string)reader.ReadObject();
            typesMap = Common.Util.SerializationUtility.DeserializeDictionary<string, Type>(reader);
            

        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            writer.WriteObject(name);
            Common.Util.SerializationUtility.SerializeDictionary(typesMap,writer);

        } 
        #endregion
    }
}
