using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System.Reflection;
using System.IO;
using System.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Lucene.Net.Analysis;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Add, "LuceneAnalyzer")]
    public class AddLuceneAnalyzerBase : AddLuceneAnalyzerParameters, IConfiguration
    {
        private string TOOLNAME = "AddLuceneAnalyzer Tool";
        private NCacheRPCService NCache = new NCacheRPCService("");
        ToolOperations toolOp = new ToolOperations();
        

        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("Error: Cache name not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(AssemblyPath))
            {
                OutputProvider.WriteErrorLine("Error: Assembly path not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Class))
            {
                OutputProvider.WriteErrorLine("Error: Class name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Name))
            {
                OutputProvider.WriteErrorLine("Error: Provider name not specified.");
                return false;
            }


            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        private void AddLuceneAnalyzer()
        {
            if (!ValidateParameters())
            {
                return;
            }

            System.Reflection.Assembly asm = null;
            Alachisoft.NCache.Config.Dom.LuceneDeployment[] prov = null;
            string failedNodes = string.Empty;
            string serverName = string.Empty;
            ICacheServer cacheServer = null;
            bool successFull = true;
            try
            {
                if (Port != -1)
                {
                    NCache.Port = Port;
                }

                if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (Server != null && Server != string.Empty)
                {
                    NCache.ServerName = Server;
                }

                try
                {
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    successFull = false;
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                    return;
                }

                if (cacheServer != null)
                {
                    serverName = cacheServer.GetClusterIP();
                    if (cacheServer.IsRunning(CacheName))
                    {
                        successFull = false;
                        throw new Exception(CacheName + " is Running on " + cacheServer.GetClusterIP() + "\nStop the cache first and try again.");
                    }
                    Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(CacheName);


                    if (serverConfig == null)
                    {
                        successFull = false;
                        throw new Exception("Specified cache is not registered on the given server.");
                    }
                    ToolsUtil.VerifyClusterConfigurations(serverConfig, CacheName);
                    try
                    {
                        asm = System.Reflection.Assembly.LoadFrom(AssemblyPath);
                    }
                    catch (Exception e)
                    {
                        successFull = false;
                        string message = string.Format("Could not load assembly \"" + AssemblyPath + "\". {0}", e.Message);
                        OutputProvider.WriteErrorLine("Error: {0}", message);
                        return;
                    }

                    if (asm == null)
                    {
                        successFull = false;
                        throw new Exception("Could not load specified assembly.");
                    }

                    if(serverConfig.CacheSettings.LuceneSettings == null)
                        serverConfig.CacheSettings.LuceneSettings = new Alachisoft.NCache.Config.Dom.LuceneSettings();

                    System.Type type = asm.GetType(Class, true);

                    if(!type.IsSubclassOf(typeof(Analyzer)))
                    {
                        successFull = false;
                        OutputProvider.WriteErrorLine("Error: Specified class does not implement Analyzer.");
                        return;
                    }
                    else
                    {
                        if (serverConfig.CacheSettings.LuceneSettings.Analyzers == null)
                        {
                            serverConfig.CacheSettings.LuceneSettings.Analyzers = new Analyzers();
                            serverConfig.CacheSettings.LuceneSettings.Analyzers.Providers = prov;
                        }
                        prov = serverConfig.CacheSettings.LuceneSettings.Analyzers.Providers;
                        serverConfig.CacheSettings.LuceneSettings.Analyzers.Providers = GetAnalyzers(GetProvider(prov, asm));
                    }

                    byte[] userId = null;
                    byte[] paswd = null;
                    if (UserId != string.Empty && Password != string.Empty)
                    {
                        userId = EncryptionUtil.Encrypt(UserId);
                        paswd = EncryptionUtil.Encrypt(Password);
                    }
                    serverConfig.ConfigVersion++;
                    if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            NCache.ServerName = node.IpAddress.ToString();
                            try
                            {
                                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                if (cacheServer.IsRunning(CacheName))
                                    throw new Exception(CacheName + " is Running on " + serverName +
                                                       "\nStop the cache first and try again.");

                                OutputProvider.WriteLine("Adding Analyzer on node '{0}' to cache '{1}'.", node.IpAddress, CacheName);
                                cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                            }
                            catch (Exception ex)
                            {
                                OutputProvider.WriteErrorLine("Failed to Lucene Analyzer on node '{0}'. ", serverName);
                                OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                                failedNodes = failedNodes + "/n" + node.IpAddress.ToString();
                                successFull = false;
                            }
                            finally
                            {
                                cacheServer.Dispose();
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            OutputProvider.WriteLine("Adding Analyzer on node '{0}' to cache '{1}'.", serverName, CacheName);
                            cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                        }
                        catch (Exception ex)
                        {
                            OutputProvider.WriteErrorLine("Failed to Lucene Analyzer on node '{0}'. ", serverName);
                            OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                            successFull = false;
                        }
                        finally
                        {
                            NCache.Dispose();
                        }
                    }
                }

            }
            catch (Exception e)
            {
                successFull = false;
                OutputProvider.WriteErrorLine("Failed to Lucene Analyzer on node '{0}'. ", NCache.ServerName);
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
            }
            finally
            {
                NCache.Dispose();
                if (successFull && !IsUsage)
                    OutputProvider.WriteLine("Analyzer successfully added");
            }
        }

        private LuceneDeployment[] GetAnalyzers(Hashtable analyzers)
        {
            LuceneDeployment[] deployments = new LuceneDeployment[analyzers.Count];
            IDictionaryEnumerator enu = analyzers.GetEnumerator();
            int index = 0;
            while(enu.MoveNext())
            {
                deployments[index] = new LuceneDeployment();
                deployments[index] = (LuceneDeployment)enu.Value;
                index++;
            }
            return deployments;
        }

        public Hashtable GetProvider(LuceneDeployment[] prov, System.Reflection.Assembly asm)
        {
            Hashtable hash = new Hashtable();
            LuceneDeployment provider = new LuceneDeployment();
            provider.AssemblyName = asm.ToString();
            provider.ClassName = Class;
            provider.FileName = asm.ManifestModule.Name;
            provider.Name = Name;
            if (prov != null)
            {
                hash = ProviderToHashtable(prov);
            }
            if (hash.Contains(provider.Name))
            {
                throw new Exception("Analyzer with the same name already exists");
            }
            hash[provider.Name] = provider;
            return hash;
        }

        private Hashtable ProviderToHashtable(LuceneDeployment[] prov)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < prov.Length; i++)
            {
                hash.Add(prov[i].Name, prov[i]);
            }
            return hash;
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }
#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName; //bin folder
                final = System.IO.Path.Combine(bin, "service"); /// from where you neeed the assemblies
            }
            if(System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string installDir = directoryInfo.Parent.FullName; //linux install directory
                directoryInfo = Directory.GetParent(installDir); //go back one directory
                installDir = directoryInfo.FullName;
                final = Path.Combine(installDir, "lib");
            }
            return System.Reflection.Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
        }
#endif
        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Add-BackingSource Cmdlet";
                AddLuceneAnalyzer();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        
    }
}
