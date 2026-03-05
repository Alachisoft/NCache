using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Alachisoft.NCache.Serialization.Formatters
{
    internal class CustomSerializationBinder : DefaultSerializationBinder
    {
        private Dictionary<string, Type> _cachedTypes = new Dictionary<string, Type>();
        public override Type BindToType(string assemblyName, string typeName)
        {
            Type type = null;
            string cacheKey = $"{typeName}${assemblyName}";

            lock (this)
            {
                if (_cachedTypes.TryGetValue(cacheKey, out type))
                    return type;

                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                type = FindAssembly(assemblies, assemblyName, typeName);

                if (type == null && (assemblyName.Contains("System.Private.CoreLib") || assemblyName.Equals("System.Collections.Concurrent")))
                {
                    // Case When NetCore List, Dictionary, ArrayList, Hashtable, DictionaryEntry, ConcurrentQueue,
                    // ConcurrentDictionary or primitive type array is being deserialized in framework application
                    assemblyName = "mscorlib";
                    typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");

                    type = FindAssembly(assemblies, assemblyName, typeName);

                    if (type == null)
                    {
                        // Case When NetCore HashSet is being deserialized in framework application
                        assemblyName = "System.Core";
                        type = FindAssembly(assemblies, assemblyName, typeName);

                        if (type == null)
                        {
                            // Case When NetCore Generic Queue is being deserialized in framework application
                            assemblyName = "System";
                            type = FindAssembly(assemblies, assemblyName, typeName);
                        }
                    }
                }

                // Case When NetCore SortedDictionary or OrderedDictionary is being deserialized in framework application
                else if (type == null && assemblyName.Contains("System.Collections"))
                {
                    assemblyName = "System";
                    typeName = typeName.Replace("System.Private.CoreLib", "mscorlib");

                    type = FindAssembly(assemblies, assemblyName, typeName);
                }

                if (type != null && !_cachedTypes.ContainsKey(cacheKey))
                    _cachedTypes.Add(cacheKey, type);
            }

            return type ?? base.BindToType(assemblyName, typeName);
        }

        private Type FindAssembly(Assembly[] assemblies, string assemblyName, string typeName)
        {
            Assembly assembly = null;
            Type type = null;

            foreach (var item in assemblies)
            {
                if (item.FullName.Equals(assemblyName) || item.GetName().Name.Equals(assemblyName))
                {
                    assembly = item;
                    break;
                }
            }

            try
            {
                assembly = assembly ?? AppDomain.CurrentDomain.Load(assemblyName);
                type = assembly.GetType(typeName);
            }
            catch { }

            return type;
        }
    }
}
