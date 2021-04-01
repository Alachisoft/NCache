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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class AssemblyDef
    {
        ModuleDefinition moduleDefinition;
        TypeDef[] types;

        public string FullName { get { return moduleDefinition.Assembly.FullName; } }

        public static AssemblyDef LoadFrom(string path)
        {
            var resolver = new DefaultAssemblyResolver();

            resolver.AddSearchDirectory(Path.GetDirectoryName(path));

            var assemblyDef = AssemblyDefinition.ReadAssembly(
                path,
                new ReaderParameters { AssemblyResolver = resolver });

            return new AssemblyDef(assemblyDef.MainModule);
        }

        public static AssemblyDef LoadFromStream(Stream filestream)
        {
            ModuleDefinition moduleDefinition = ModuleDefinition.ReadModule(filestream);
            return new AssemblyDef(moduleDefinition);
        }

        public AssemblyDef(ModuleDefinition moduleDefinition)
        {
            this.moduleDefinition = moduleDefinition;
        }

        public TypeDef GetType(string fullName)
        {
            return new TypeDef(this.moduleDefinition.GetType(fullName));
        }
        public TypeDef[] GetTypes()
        {
            if (types == null)
            {
                List<TypeDef> typeList = new List<TypeDef>();
                foreach (TypeDefinition typDef in this.moduleDefinition.Types)
                {
                    typeList.Add(new TypeDef(typDef));
                }

                types = typeList.ToArray();
            }

            return types;
        }
    }
}
