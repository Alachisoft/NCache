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
using System.Collections.Generic;
using Alachisoft.NCache.Common.AssemblyBrowser;

namespace Alachisoft.NCache.Common
{
    [Serializable]
    public class DiscoverAssembly : MarshalByRefObject, IDiscoverAssembly
    {
        public string[] GetClassTypeNames(string assemblyPath)
        {
            return GetClassTypeNames(assemblyPath, null);
        }

        public string[] GetClassTypeNames(string assemblyPath, ClassTypeFilter filter)
        {
            List<string> typeNames = new List<string>();
            AssemblyDef assembly = AssemblyDef.LoadFrom(assemblyPath);
            foreach (TypeDef type in assembly.GetTypes())
            {
                if (filter != null)
                {
                    if (filter.FilterType(type))                    
                        typeNames.Add(type.FullName);
                }
                else
                    typeNames.Add(type.FullName);
            }
            return typeNames.ToArray();
        }

        public string[] GetClassTypeNames(string[] assemblyPaths)
        {
            return GetClassTypeNames(assemblyPaths, null);
        }

        public string[] GetClassTypeNames(string[] assemblyPaths, ClassTypeFilter filter)
        {
            List<string> typeNames =new List<string>();;
            AssemblyDef assembly;

            foreach (string path in assemblyPaths)
            {
                assembly = AssemblyDef.LoadFrom(path);                
                foreach (TypeDef type in assembly.GetTypes())
                {
                    if (filter != null)
                    {
                        if (filter.FilterType(type))
                            typeNames.Add(type.FullName);
                    }
                    else
                        typeNames.Add(type.FullName);
                }
            }
            return typeNames.ToArray();
        }

    
        public TypeDef[] GetTypes(string assemblyPath)
        {
            return GetTypes(assemblyPath, null);
        }

        public TypeDef[] GetTypes(string assemblyPath, ClassTypeFilter filter)
        {
            AssemblyDef assembly = AssemblyDef.LoadFrom(assemblyPath);
            TypeDef[] types = assembly.GetTypes();
            List<TypeDef> typesList = new List<TypeDef>();
            foreach (TypeDef t in types)
            {
                if (filter != null)
                {
                    if (filter.FilterType(t))
                        typesList.Add(t);
                }
                else
                    typesList.Add(t);
            }
            return typesList.ToArray();
        }

        public Dictionary<string, TypeDef[]> GetTypes(string[] assemblyPaths)
        {
            return GetTypes(assemblyPaths, null);
        }

        public Dictionary<string, TypeDef[]> GetTypes(string[] assemblyPaths, ClassTypeFilter filter)
        {
            Dictionary<string, TypeDef[]> dictionary = new Dictionary<string, TypeDef[]>();
            TypeDef[] types;
            List<TypeDef> typesList;
            foreach (string path in assemblyPaths)
            {
                AssemblyDef assembly = AssemblyDef.LoadFrom(path);
                types = assembly.GetTypes();
                typesList = new List<TypeDef>();
                foreach (TypeDef t in types)
                {
                    if (filter != null)
                    {
                        if (filter.FilterType(t))
                            typesList.Add(t);
                    }
                    else
                        typesList.Add(t);
                }
                dictionary.Add(path, typesList.ToArray());
            }
            return dictionary;
        }


        public string AssemblyFullName(string assemblyPath)
        {
            AssemblyDef assembly = AssemblyDef.LoadFrom(assemblyPath);
            return assembly.FullName;
        }

        public string[] AssemblyFullName(string[] assemblyPath)
        {
            List<string> fullNames = new List<string>();
            AssemblyDef assembly;
            foreach (string path in assemblyPath)
            {
                assembly = AssemblyDef.LoadFrom(path);
                fullNames.Add(assembly.FullName);
            }
            return fullNames.ToArray();
        }
    }
}
