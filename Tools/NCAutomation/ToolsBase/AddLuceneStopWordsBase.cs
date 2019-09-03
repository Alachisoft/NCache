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
using Alachisoft.NCache.Common.Queries.Lucene;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Add, "LuceneStopWords")]
    public class AddLuceneStopWordsBase : AddLuceneStopWordsParameters , IConfiguration
    {
        private string TOOLNAME = "AddLuceneStopWords Tool";
        private NCacheRPCService NCache = new NCacheRPCService("");
        ToolOperations toolOp = new ToolOperations();
        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("Error: Cache name not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(FilePath))
            {
                OutputProvider.WriteErrorLine("Error: File path not specified.");
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


        private void AddLuceneStopWords()
        {
            if (!ValidateParameters())
            {
                return;
            }

            System.IO.FileInfo fInfo = null;
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
                        fInfo = new System.IO.FileInfo(FilePath);
                    }
                    catch (Exception e)
                    {

                        successFull = false;
                        string message = string.Format("Could not load file \"" + FilePath + "\". {0}", e.Message);
                        OutputProvider.WriteErrorLine("Error: {0}", message);
                        return;
                    }
                    
                    if(serverConfig.CacheSettings.LuceneSettings == null)
                        serverConfig.CacheSettings.LuceneSettings = new Alachisoft.NCache.Config.Dom.LuceneSettings();

                    if(fInfo.Extension != ".txt")
                    {
                        successFull = false;
                        string message = string.Format("\"" + FilePath + "\" is not a Text file. {0}");
                        OutputProvider.WriteErrorLine("Error: {0}", message);
                        return;
                    }

                    if (serverConfig.CacheSettings.LuceneSettings.StopWordFiles == null)
                        serverConfig.CacheSettings.LuceneSettings.StopWordFiles = new StopWords();

                    if (serverConfig.CacheSettings.LuceneSettings.StopWordFiles.Providers != null)
                        prov = serverConfig.CacheSettings.LuceneSettings.StopWordFiles.Providers;

                    serverConfig.CacheSettings.LuceneSettings.StopWordFiles.Providers = GetStopWords(GetProvider(prov, fInfo));

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

                                OutputProvider.WriteLine("Adding Lucene Stop Words file on node '{0}' to cache '{1}'.", node.IpAddress, CacheName);
                                cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                            }
                            catch (Exception ex)
                            {
                                OutputProvider.WriteErrorLine("Failed to Add Lucene Stop Words file on '{0}'. ", serverName);
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
                            OutputProvider.WriteLine("Adding Lucene Stop Words file on node '{0}' to cache '{1}'.", serverName, CacheName);
                            cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                        }
                        catch (Exception ex)
                        {
                            OutputProvider.WriteErrorLine("Failed to Add Lucene Stop Words file on '{0}'. ", serverName);
                            OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                            successFull = false;
                        }
                        finally
                        {
                            NCache.Dispose();
                            if (successFull && !IsUsage)
                                OutputProvider.WriteLine("Stop words file successfully added");
                        }
                    }
                }
            }
            catch (Exception)
            {
                successFull = false;
                throw;
            }
            finally
            {
                NCache.Dispose();
                if (successFull && !IsUsage)
                    OutputProvider.WriteLine("Stop words file successfully added");
            }
        }

        private LuceneDeployment[] GetStopWords(Hashtable stopWords)
        {
            LuceneDeployment[] deployments = new LuceneDeployment[stopWords.Count];
            IDictionaryEnumerator enu = stopWords.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {
                deployments[index] = new LuceneDeployment();
                deployments[index] = (LuceneDeployment)enu.Value;
                index++;
            }
            return deployments;
        }

        private Hashtable GetProvider(LuceneDeployment[] prov, FileInfo fInfo)
        {
            Hashtable hash = new Hashtable();
            LuceneDeployment provider = new LuceneDeployment();
            provider.Name = Name;
            provider.FileName = fInfo.Name;
            if(prov != null)
            {
                hash = ProviderToHashTable(prov);
            }
            if(hash.Contains(provider.Name))
                throw new Exception("Stop Words with the same name already exists");

            if(LuceneUtil.Providers.IsPreDefined(provider.Name, LuceneUtil.Providers.Type.StopWords))
                throw new Exception("Provider name is invalid");

            hash[provider.Name] = provider;
            return hash;
        }

        private Hashtable ProviderToHashTable(LuceneDeployment[] prov)
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
                AddLuceneStopWords();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

    }
}
