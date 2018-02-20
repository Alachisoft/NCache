// Copyright (c) 2018 Alachisoft
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
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class TypeDef
    {
        TypeReference typeReference;

        PropertyDef[] properties;
        FieldDef[] fields;

        public string Name { get { return typeReference.Name; } }
        public string FullName { get { return typeReference.FullName; } }
        public string Namespace { get { return typeReference.Namespace; } }
        public bool IsGenericType { get { return typeReference.IsGenericInstance; } }
        public bool IsPrimitive { get { return typeReference.IsPrimitive; } }
        public bool IsValueType { get { return typeReference.IsValueType; } }
        public string AssemblyFullName { get { return typeReference.Module.Assembly.FullName; } }
        public TypeDef BaseType
        {
            get
            {
                if (typeReference is TypeDefinition && ((TypeDefinition)typeReference).BaseType != null)
                    return new TypeDef(((TypeDefinition)typeReference).BaseType);

                return null;
            }
        }
        public bool IsClass
        {
            get
            {
                if (typeReference is TypeDefinition)
                    return ((TypeDefinition)typeReference).IsClass;

                return false;
            }
        }

     
        public TypeDef(TypeReference typeReference)
        {
            this.typeReference = typeReference;
        }

        public ICollection GetGenericArguments()
        {
            return this.typeReference.GenericParameters;
        }

        public PropertyDef[] GetProperties()
        {
            if (properties == null)
            {
                List<PropertyDef> propertyList = new List<PropertyDef>();
                TypeDefinition typeDefinition = (TypeDefinition)typeReference;

                if(typeDefinition != null)
                    foreach (PropertyDefinition prop in typeDefinition.Properties)
                    {
                        propertyList.Add(new PropertyDef(prop));
                    }

                properties = propertyList.ToArray();
            }

            return properties;
        }

        public FieldDef[] GetFields()
        {
            if (fields == null)
            {
                List<FieldDef> fieldList = new List<FieldDef>();
                TypeDefinition typeDefinition = (TypeDefinition)typeReference;

                if (typeDefinition != null)
                    foreach (FieldDefinition field in typeDefinition.Fields)
                    {
                        fieldList.Add(new FieldDef(field));
                    }

                fields = fieldList.ToArray();
            }

            return fields;
        }

        public PropertyDef GetProperty(string name)
        {
            foreach (PropertyDef property in this.GetProperties())
            {
                if (property.Name == name) return property;
            }

            return null;
        }

        public FieldDef GetField(string name)
        {
            foreach (FieldDef field in this.GetFields())
            {
                if (field.Name == name) return field;
            }

            return null;
        }

        public object GetCustomAttributeValue(string attributeName)
        {
            foreach (CustomAttribute ca in this.typeReference.Module.Assembly.CustomAttributes)
                if (attributeName == ca.AttributeType.FullName)
                    return ca.ConstructorArguments[0].Value;

            return null;
        }

        public bool HasDefaultConstructor()
        {
            TypeDefinition typeDefinition = (TypeDefinition)typeReference;
            if (typeDefinition != null)
                foreach (MethodDefinition method in typeDefinition.Methods)
                {
                    if (method.Name == ".ctor" && method.Parameters.Count == 0)
                        return true;
                }

            return false;
        }

        public bool ImplementsInterface(Type interfaceType)
        {
            TypeDefinition typeDefinition = (TypeDefinition)typeReference;
            if (typeDefinition != null)
                foreach (TypeReference tr in typeDefinition.Interfaces)
                {
                    string fullName = System.Reflection.Assembly.CreateQualifiedName(tr.Module.Assembly.FullName, tr.FullName);
                    Type intf = Type.GetType(fullName, false);
                    if (intf == interfaceType)
                        return true;
                }

            return false;
        }


    }
}
