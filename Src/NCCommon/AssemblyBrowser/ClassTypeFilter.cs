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

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    [Serializable]
    public class ClassTypeFilter: Runtime.Serialization.ICompactSerializable
    {
        Type _typeToFilter;

        public ClassTypeFilter(Type typeToFilter)
        {
            _typeToFilter = typeToFilter;
        }
        public virtual bool FilterType(TypeDef type)
        {
            return type.IsAssignableTo(_typeToFilter);
        }
        public virtual bool FilterType(Type type)
        {
            return _typeToFilter.IsAssignableFrom(type);
        }

        #region ICompact Serializable
        public void Deserialize(Runtime.Serialization.IO.CompactReader reader)
        {
            _typeToFilter = (Type)reader.ReadObject();
        }

        public void Serialize(Runtime.Serialization.IO.CompactWriter writer)
        {
            writer.WriteObject(_typeToFilter);
        } 
        #endregion
    }
}