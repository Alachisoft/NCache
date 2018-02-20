// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Core.EntityClient;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Core.Metadata.Edm;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace Alachisoft.NCache.Integrations.EntityFramework.Toolkit
{
    /// <summary>
    /// Utilities for creating <see cref="EntityConnection"/> with wrapped providers.
    /// </summary>
    public static class EntityConnectionWrapperUtils
    {
        private static Dictionary<string, MetadataWorkspace> metadataWorkspaceMemoizer = new Dictionary<string, MetadataWorkspace>();
        private static byte[] systemPublicKeyToken = { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A };
        private static Regex resRegex = new Regex(@"^res://(?<assembly>.*)/(?<resource>.*)$");

        /// <summary>
        /// Creates the entity connection with wrappers.
        /// </summary>
        /// <param name="entityConnectionString">The original entity connection string.</param>
        /// <param name="wrapperProviders">List for wrapper providers.</param>
        /// <returns>EntityConnection object which is based on a chain of providers.</returns>
        public static EntityConnection CreateEntityConnectionWithWrappers(string entityConnectionString, params string[] wrapperProviders)
        {
            EntityConnectionStringBuilder ecsb = new EntityConnectionStringBuilder(entityConnectionString);

            // if connection string is name=EntryName, look up entry in the config file and parse it
            if (!ecsb.Name.IsNullOrEmpty())
            {
                var connStr = System.Configuration.ConfigurationManager.ConnectionStrings[ecsb.Name];
                if (connStr == null)
                {
                    throw new ArgumentException("Specified named connection string '" + ecsb.Name + "' was not found in the configuration file.");
                }

                ecsb.ConnectionString = connStr.ConnectionString;
            }

            MetadataWorkspace workspace;
            if (!metadataWorkspaceMemoizer.TryGetValue(ecsb.ConnectionString, out workspace))
            {
                workspace = CreateWrappedMetadataWorkspace(ecsb.Metadata, wrapperProviders);
                metadataWorkspaceMemoizer.Add(ecsb.ConnectionString, workspace);
            }

            var storeConnection = DbProviderFactories.GetFactory(ecsb.Provider).CreateConnection();
            storeConnection.ConnectionString = ecsb.ProviderConnectionString;
            var newEntityConnection = new EntityConnection(workspace, DbConnectionWrapper.WrapConnection(storeConnection, wrapperProviders));
            return newEntityConnection;
        }

        private static MetadataWorkspace CreateWrappedMetadataWorkspace(string metadata, IEnumerable<string> wrapperProviderNames)
        {
            MetadataWorkspace workspace = new MetadataWorkspace();

            // parse Metadata keyword and load CSDL,SSDL,MSL files into XElement structures...
            var csdl = new List<XElement>();
            var ssdl = new List<XElement>();
            var msl = new List<XElement>();
            ParseMetadata(metadata, csdl, ssdl, msl);

            // fix all SSDL files by changing 'Provider' to our provider and modifying
            foreach (var ssdlFile in ssdl)
            {
                foreach (string providerName in wrapperProviderNames)
                {
                    ssdlFile.Attribute("ProviderManifestToken").Value = ssdl[0].Attribute("Provider").Value + ";" + ssdlFile.Attribute("ProviderManifestToken").Value;
                    ssdlFile.Attribute("Provider").Value = providerName;
                }
            }

            // load item collections from XML readers created from XElements...
            EdmItemCollection eic = new EdmItemCollection(csdl.Select(c => c.CreateReader()));
            StoreItemCollection sic = new StoreItemCollection(ssdl.Select(c => c.CreateReader()));
            StorageMappingItemCollection smic = new StorageMappingItemCollection(eic, sic, msl.Select(c => c.CreateReader()));

            // and create metadata workspace based on them.
            workspace = new MetadataWorkspace();
            workspace.RegisterItemCollection(eic);
            workspace.RegisterItemCollection(sic);
            workspace.RegisterItemCollection(smic);
            return workspace;
        }

        private static void ParseMetadata(string metadata, List<XElement> csdl, List<XElement> ssdl, List<XElement> msl)
        {
            foreach (string component in metadata.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(c => c.Trim()))
            {
                string translatedComponent = component;

                if (translatedComponent.StartsWith("~", StringComparison.Ordinal))
                {
                    HttpContext context = HttpContext.Current;
                    if (context == null)
                    {
                        throw new NotSupportedException("Paths prefixed with '~' are not supported outside of ASP.NET.");
                    }

                    translatedComponent = context.Server.MapPath(translatedComponent);
                }

                if (translatedComponent.StartsWith("res://", StringComparison.Ordinal))
                {
                    ParseResources(translatedComponent, csdl, ssdl, msl);
                }
                else if (Directory.Exists(translatedComponent))
                {
                    ParseDirectory(translatedComponent, csdl, ssdl, msl);
                }
                else if (translatedComponent.EndsWith(".csdl", StringComparison.OrdinalIgnoreCase))
                {
                    csdl.Add(XElement.Load(translatedComponent));
                }
                else if (translatedComponent.EndsWith(".ssdl", StringComparison.OrdinalIgnoreCase))
                {
                    ssdl.Add(XElement.Load(translatedComponent));
                }
                else if (translatedComponent.EndsWith(".msl", StringComparison.OrdinalIgnoreCase))
                {
                    msl.Add(XElement.Load(translatedComponent));
                }
                else
                {
                    throw new NotSupportedException("Unknown metadata component: " + component);
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "We want to ignore exceptions during loading of references.")]
        private static void ParseResources(string resPath, List<XElement> csdl, List<XElement> ssdl, List<XElement> msl)
        {
            Match match = resRegex.Match(resPath);
            if (!match.Success)
            {
                throw new NotSupportedException("Not supported resource path: " + resPath);
            }

            string assemblyName = match.Groups["assembly"].Value;
            string resourceName = match.Groups["resource"].Value;

            List<Assembly> assembliesToConsider = new List<Assembly>();
            if (assemblyName == "*")
            {
                assembliesToConsider.AddRange(AppDomain.CurrentDomain.GetAssemblies());
            }
            else
            {
                assembliesToConsider.Add(Assembly.Load(new AssemblyName(assemblyName)));
            }

            var domainManager = AppDomain.CurrentDomain.DomainManager;
            if (domainManager != null && domainManager.EntryAssembly != null)
            {
                foreach (AssemblyName asmName in domainManager.EntryAssembly.GetReferencedAssemblies())
                {
                    try
                    {
                        var asm = Assembly.Load(asmName);
                        if (!assembliesToConsider.Contains(asm))
                        {
                            assembliesToConsider.Add(asm);
                        }
                    }
                    catch
                    {
                        // ignore errors
                    }
                }
            }

            foreach (Assembly asm in assembliesToConsider.Where(asm => !IsEcmaAssembly(asm) && !IsSystemAssembly(asm)))
            {
                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        continue;
                    }

                    if (resourceName.EndsWith(".csdl", StringComparison.OrdinalIgnoreCase))
                    {
                        csdl.Add(XElement.Load(XmlReader.Create(stream)));
                        return;
                    }
                    else if (resourceName.EndsWith(".ssdl", StringComparison.OrdinalIgnoreCase))
                    {
                        ssdl.Add(XElement.Load(XmlReader.Create(stream)));
                        return;
                    }
                    else if (resourceName.EndsWith(".msl", StringComparison.OrdinalIgnoreCase))
                    {
                        msl.Add(XElement.Load(XmlReader.Create(stream)));
                        return;
                    }
                }
            }

            throw new InvalidOperationException("Resource " + resPath + " not found.");
        }

        private static bool IsEcmaAssembly(Assembly asm)
        {
            byte[] publicKey = asm.GetName().GetPublicKey();

            // ECMA key is special, as it is only 4 bytes long
            if (publicKey != null && publicKey.Length == 16 && publicKey[8] == 0x4)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool IsSystemAssembly(Assembly asm)
        {
            byte[] publicKeyToken = asm.GetName().GetPublicKeyToken();

            if (publicKeyToken != null && publicKeyToken.Length == systemPublicKeyToken.Length)
            {
                for (int i = 0; i < systemPublicKeyToken.Length; ++i)
                {
                    if (systemPublicKeyToken[i] != publicKeyToken[i])
                    {
                        return false;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        private static void ParseDirectory(string directory, List<XElement> csdl, List<XElement> ssdl, List<XElement> msl)
        {
            foreach (string file in Directory.GetFiles(directory, "*.csdl"))
            {
                csdl.Add(XElement.Load(file));
            }

            foreach (string file in Directory.GetFiles(directory, "*.ssdl"))
            {
                ssdl.Add(XElement.Load(file));
            }

            foreach (string file in Directory.GetFiles(directory, "*.msl"))
            {
                msl.Add(XElement.Load(file));
            }
        }

    }
}
