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
    public class FieldDef
    {
        FieldInfo fieldInfo;
        TypeDef fieldTypeDef;

        public FieldDef(FieldInfo fieldInfo)
        {
            this.fieldInfo = fieldInfo;
        }

        public string Name
        {
            get
            {
                return this.fieldInfo.Name;
            }
        }

        /// <summary>
        /// Signature of the field as the assembly metadata spells it, i.e.
        /// "FieldType DeclaringType::Name".
        /// </summary>
        public string FullName
        {
            get
            {
                return string.Concat(
                    TypeDef.GetFullName(this.fieldInfo.FieldType),
                    " ",
                    TypeDef.GetFullName(this.fieldInfo.DeclaringType),
                    "::",
                    this.fieldInfo.Name);
            }
        }

        public TypeDef FieldType
        {
            get
            {
                if (fieldTypeDef == null)
                    fieldTypeDef = new TypeDef(this.fieldInfo.FieldType);

                return fieldTypeDef;
            }
        }
    }
}
