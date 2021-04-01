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
using Newtonsoft.Json;
using Runtime = Alachisoft.NCache.Runtime;


namespace Alachisoft.NCache.Config.Dom
{
    [Serializable]
    public class GenericArgumentType : ICloneable,Runtime.Serialization.ICompactSerializable
    {
        #region [Attributes]
        private string id;
        private CompactClass[] _genericArgsCompactTypes;
        private List<CompactClass> _genericArgsCompactTypeList;
        #endregion
        
        public GenericArgumentType() { }

        #region [Properties]
        [ConfigurationAttribute("id")]
        public string ID
        {
            get { return id; }
            set { id = value; }
        }

        [ConfigurationSection("compact-class")]
        public CompactClass[] GenericArgsCompactTypes
        {
            get
            {
                if (_genericArgsCompactTypeList != null)
                    return _genericArgsCompactTypeList.ToArray();
                return null;
            }
            set
            {
                if (_genericArgsCompactTypeList == null)
                    _genericArgsCompactTypeList = new List<CompactClass>();

                _genericArgsCompactTypeList.Clear();
                if (value != null)
                {
                    _genericArgsCompactTypeList.AddRange(value);
                }
            }
        }
        [JsonIgnore]
        public List<CompactClass> GenericArgsCompactTypeList
        {
            get { return _genericArgsCompactTypeList; }
            set { _genericArgsCompactTypeList = value; }
        }
        #endregion

        #region ICloneable Members

        public object Clone()
        {
            GenericArgumentType genericArgType = new GenericArgumentType();
            genericArgType.ID = ID != null ? (string)ID.Clone() : null;
            genericArgType.GenericArgsCompactTypes = GenericArgsCompactTypes != null ? (CompactClass[])GenericArgsCompactTypes.Clone() : null;
            return genericArgType;           
        }

        #endregion


        #region  ICompact Serializable Members

        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            id = (string)reader.ReadObject();
            
            _genericArgsCompactTypeList = Common.Util.SerializationUtility.DeserializeList<CompactClass>(reader);

            bool flag = reader.ReadBoolean();
            if (flag)
            {
                int length = reader.ReadInt32();
                _genericArgsCompactTypes = new CompactClass[length];

                for (int i = 0; i < length; i++)
                {
                    _genericArgsCompactTypes[i] = (CompactClass)reader.ReadObject();
                }
                
            }
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(id);
            
            Common.Util.SerializationUtility.SerializeList<CompactClass>(_genericArgsCompactTypeList,writer);

            if (_genericArgsCompactTypes != null)
            {
                writer.Write(true);
                writer.Write(_genericArgsCompactTypes.Length);

                for (int i = 0; i < _genericArgsCompactTypes.Length; i++)
                {
                    writer.WriteObject(_genericArgsCompactTypes[i]);
                }
            }

            else
            {
                writer.Write(false);
            }
        } 
        #endregion
    }
}
