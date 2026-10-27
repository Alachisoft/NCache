//  Copyright (c) 2026 Alachisoft
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
using System.Reflection;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class PropertyDef
    {
        PropertyInfo propertyInfo;
        TypeDef propertyTypeDef;

        public PropertyDef(PropertyInfo propertyInfo)
        {
            this.propertyInfo = propertyInfo;
        }

        public string Name
        {
            get
            {
                return this.propertyInfo.Name;
            }
        }

        /// <summary>
        /// Signature of the property as the assembly metadata spells it, i.e.
        /// "PropertyType DeclaringType::Name".
        /// </summary>
        public string FullName
        {
            get
            {
                return string.Concat(
                    TypeDef.GetFullName(this.propertyInfo.PropertyType),
                    " ",
                    TypeDef.GetFullName(this.propertyInfo.DeclaringType),
                    "::",
                    this.propertyInfo.Name);
            }
        }

        public TypeDef PropertyType
        {
            get
            {
                if (propertyTypeDef == null)
                    propertyTypeDef = new TypeDef(this.propertyInfo.PropertyType);

                return propertyTypeDef;
            }
        }
    }
}
