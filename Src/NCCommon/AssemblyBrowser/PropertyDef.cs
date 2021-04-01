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
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class PropertyDef
    {
        PropertyDefinition propertyDefinition;
        TypeDef propertyTypeDef;
        public PropertyDef(PropertyDefinition propertyDefinition)
        {
            this.propertyDefinition = propertyDefinition;
        }

        public string Name
        {
            get
            {
                return this.propertyDefinition.Name;
            }
        }

        public string FullName
        {
            get
            {
                return this.propertyDefinition.FullName;
            }
        }

        public TypeDef PropertyType
        {
            get
            {
                if(propertyTypeDef == null)
                    propertyTypeDef = new TypeDef(this.propertyDefinition.PropertyType);

                return propertyTypeDef;
            }
        }
    }
}
