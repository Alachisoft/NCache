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
// limitations under the License

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
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
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            foreach (Type type in assembly.GetTypes())
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
            Assembly assembly;

            foreach (string path in assemblyPaths)
            {
                assembly = Assembly.LoadFrom(path);                
                foreach (Type type in assembly.GetTypes())
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

    
        public Type[] GetTypes(string assemblyPath)
        {
            return GetTypes(assemblyPath, null);
        }

        public Type[] GetTypes(string assemblyPath, ClassTypeFilter filter)
        {
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            Type[] types = assembly.GetTypes();
            List<Type> typesList = new List<Type>();
            foreach (Type t in types)
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

        public Dictionary<string, Type[]> GetTypes(string[] assemblyPaths)
        {
            return GetTypes(assemblyPaths, null);
        }

        public Dictionary<string, Type[]> GetTypes(string[] assemblyPaths, ClassTypeFilter filter)
        {
            Dictionary<string, Type[]> dictionary = new Dictionary<string, Type[]>();
            Type[] types;
            List<Type> typesList;
            foreach (string path in assemblyPaths)
            {
                Assembly assembly = Assembly.LoadFrom(path);
                types = assembly.GetTypes();
                typesList = new List<Type>();
                foreach (Type t in types)
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
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly.FullName;
        }

        public string[] AssemblyFullName(string[] assemblyPath)
        {
            List<string> fullNames = new List<string>();
            Assembly assembly;
            foreach (string path in assemblyPath)
            {
                assembly = Assembly.LoadFrom(path);
                fullNames.Add(assembly.FullName);
            }
            return fullNames.ToArray();
        }
    }
}
