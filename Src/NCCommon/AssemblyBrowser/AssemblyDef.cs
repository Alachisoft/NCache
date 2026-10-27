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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Alachisoft.NCache.Common.AssemblyBrowser
{
    public class AssemblyDef
    {
        // Assemblies are inspected as metadata only; they are never loaded into the
        // execution context of the process doing the inspection. The context has to
        // outlive every Type handed out by this instance, so it is kept alive here.
        MetadataLoadContext metadataContext;
        Assembly assembly;
        TypeDef[] types;

        public string FullName { get { return assembly.FullName; } }

        public static AssemblyDef LoadFrom(string path)
        {
            var resolver = new ProbingAssemblyResolver(GetProbingDirectories(Path.GetDirectoryName(path)));

            var metadataContext = new MetadataLoadContext(resolver);

            return new AssemblyDef(metadataContext, metadataContext.LoadFromAssemblyPath(path));
        }

        public static AssemblyDef LoadFromStream(Stream filestream)
        {
            var resolver = new ProbingAssemblyResolver(GetProbingDirectories(null));

            var metadataContext = new MetadataLoadContext(resolver);

            return new AssemblyDef(metadataContext, metadataContext.LoadFromStream(filestream));
        }

        internal AssemblyDef(MetadataLoadContext metadataContext, Assembly assembly)
        {
            this.metadataContext = metadataContext;
            this.assembly = assembly;
        }

        public IList<CustomAttributeData> CustomAttributes
        {
            get { return this.assembly.GetCustomAttributesData(); }
        }

        public string AssemblyFileVersion
        {
            get
            {
                var fileVersionAttrib = CustomAttributes.First(ca => ca.AttributeType.Name == "AssemblyFileVersionAttribute");
                var constArg = fileVersionAttrib.ConstructorArguments.First();
                return constArg.Value.ToString();
            }
        }

        public TypeDef GetType(string fullName)
        {
            return new TypeDef(this.assembly.GetType(fullName));
        }

        public TypeDef[] GetTypes()
        {
            if (types == null)
            {
                List<TypeDef> typeList = new List<TypeDef>();
                foreach (Type type in GetDefinedTypes())
                {
                    // only top level types are listed, nested ones are reached through
                    // their declaring type.
                    if (type.IsNested) continue;

                    typeList.Add(new TypeDef(type));
                }

                types = typeList.ToArray();
            }

            return types;
        }

        private Type[] GetDefinedTypes()
        {
            try
            {
                return this.assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // a type whose base type or interface lives in an assembly that was not
                // deployed along side the inspected one cannot be built; the remaining
                // types are still usable.
                return ex.Types.Where(type => type != null).ToArray();
            }
        }

        /// <summary>
        /// Builds the folders the metadata reader probes for referenced assemblies. It
        /// mirrors what the previous implementation searched: the folder the inspected
        /// assembly lives in, the folders the process doing the inspection loads from,
        /// and the framework the process is running on.
        /// </summary>
        private static List<string> GetProbingDirectories(string assemblyDirectory)
        {
            List<string> directories = new List<string>();

            AddDirectory(directories, assemblyDirectory);
            AddDirectory(directories, AppContext.BaseDirectory);

            // The tools do not necessarily run out of the folder that holds the NCache
            // assemblies: the PowerShell module lives in bin\tools\ncacheps while the
            // contracts a provider implements sit in bin\tools\libs, and the host of the
            // process is PowerShell itself. Probing the folders this process has actually
            // loaded assemblies from covers those layouts.
            foreach (string directory in GetLoadedAssemblyDirectories())
                AddDirectory(directories, directory);

            // the core assembly (mscorlib/System.Runtime/System.Private.CoreLib) has to be
            // reachable, the metadata reader cannot resolve System.Object without it.
            string runtimeDirectory = GetRuntimeDirectory();
            AddDirectory(directories, runtimeDirectory);

            if (runtimeDirectory != null)
                AddDirectory(directories, Path.Combine(runtimeDirectory, "Facades"));

            return directories;
        }

        private static string GetRuntimeDirectory()
        {
            try
            {
                string location = typeof(object).Assembly.Location;

                if (string.IsNullOrEmpty(location)) return null;

                return Path.GetDirectoryName(location);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static IEnumerable<string> GetLoadedAssemblyDirectories()
        {
            List<string> directories = new List<string>();

            foreach (Assembly loaded in GetLoadedAssemblies())
            {
                string location = GetLocation(loaded);

                if (location == null) continue;

                try
                {
                    directories.Add(Path.GetDirectoryName(location));
                }
                catch (Exception)
                {
                }
            }

            return directories;
        }

        private static Assembly[] GetLoadedAssemblies()
        {
            try
            {
                return AppDomain.CurrentDomain.GetAssemblies();
            }
            catch (Exception)
            {
                return new Assembly[0];
            }
        }

        private static string GetLocation(Assembly assembly)
        {
            try
            {
                // a dynamic or single file assembly has no location to probe
                if (assembly == null || assembly.IsDynamic) return null;

                string location = assembly.Location;

                return string.IsNullOrEmpty(location) ? null : location;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void AddDirectory(List<string> directories, string directory)
        {
            if (string.IsNullOrEmpty(directory)) return;

            foreach (string existing in directories)
            {
                if (string.Equals(existing, directory, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            directories.Add(directory);
        }

        /// <summary>
        /// Resolves a referenced assembly by probing a list of folders, the way the
        /// previous implementation did, rather than from a fixed set of files.
        /// </summary>
        private sealed class ProbingAssemblyResolver : MetadataAssemblyResolver
        {
            private readonly List<string> directories;

            internal ProbingAssemblyResolver(List<string> directories)
            {
                this.directories = directories;
            }

            public override Assembly Resolve(MetadataLoadContext context, AssemblyName assemblyName)
            {
                string name = assemblyName == null ? null : assemblyName.Name;

                if (string.IsNullOrEmpty(name)) return null;

                foreach (string directory in directories)
                {
                    Assembly resolved = TryLoad(context, Path.Combine(directory, name + ".dll"));

                    if (resolved != null) return resolved;
                }

                // the reference may be satisfied by an assembly this process loaded from a
                // folder that was not on the list, e.g. one probed at run time.
                foreach (Assembly loaded in GetLoadedAssemblies())
                {
                    if (!IsNamed(loaded, name)) continue;

                    Assembly resolved = TryLoad(context, GetLocation(loaded));

                    if (resolved != null) return resolved;
                }

                return null;
            }

            private static bool IsNamed(Assembly assembly, string name)
            {
                try
                {
                    return assembly != null
                        && string.Equals(assembly.GetName().Name, name, StringComparison.OrdinalIgnoreCase);
                }
                catch (Exception)
                {
                    return false;
                }
            }

            private static Assembly TryLoad(MetadataLoadContext context, string path)
            {
                if (string.IsNullOrEmpty(path)) return null;

                try
                {
                    if (!File.Exists(path)) return null;

                    return context.LoadFromAssemblyPath(path);
                }
                catch (Exception)
                {
                    // not a managed assembly, unreadable, or a duplicate of one already
                    // loaded into the context under a different path.
                    return null;
                }
            }
        }
    }
}
