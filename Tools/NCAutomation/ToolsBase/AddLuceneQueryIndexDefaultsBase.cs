using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Text;
using Alachisoft.NCache.Caching.Queries.Lucene;
using Alachisoft.NCache.Caching.Queries.Lucene.Util;
using Alachisoft.NCache.Common.AssemblyBrowser;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Add, "LuceneQueryIndexDefaults")]
    public class AddLuceneQueryIndexDefaultsBase : AddLuceneQueryIndexDefaultsParameters, IConfiguration
    {
        private string TOOLNAME = "AddLuceneQueryIndexDefaults Tool";
        private NCacheRPCService NCache = new NCacheRPCService("");
        private Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig;
        private ICacheServer cacheServer;
        private LuceneAttributes defaults;
        ToolOperations toolOp = new ToolOperations();

        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("\nError: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(AssemblyPath))
            {
                OutputProvider.WriteErrorLine("\nError: Assembly Path not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Class))
            {
                OutputProvider.WriteErrorLine("\nError: Class name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(LuceneType))
            {
                OutputProvider.WriteErrorLine("\nError: Lucene Type not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Analyzer))
            {
                OutputProvider.WriteErrorLine("\nError: Analyzer not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(TermVector))
            {
                OutputProvider.WriteErrorLine("\nError: Term Vector not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(Pattern))
            {
                OutputProvider.WriteErrorLine("\nError: Pattern not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(StopWords))
            {
                OutputProvider.WriteErrorLine("\nError: Stop Words not specified.");
                return false;
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        private void AddLuceneQueryIndexDefaults()
        {
            if (!ValidateParameters())
                return;
            bool successful = true;
            AssemblyDef asm = null;
            //ArrayList cc = new ArrayList();
            Alachisoft.NCache.Config.Dom.Class[] queryClasses = null;
            string failedNodes = string.Empty;
            string serverName = string.Empty;

            try
            {

                if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (Server != null && Server != string.Empty)
                {

                    NCache.ServerName = Server;
                }
                if (Port != -1)
                {
                    NCache.Port = Port;
                }
                try
                {
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                    return;
                }

                ToolsUtil.VerifyClusterConfigurations(cacheServer.GetNewConfiguration(CacheName), CacheName);


                string extension = ".dll";
                if (cacheServer != null)
                {
                    serverName = cacheServer.GetClusterIP();
                    if (cacheServer.IsRunning(CacheName))
                        throw new Exception(CacheName + " is Running on " + serverName +
                                            "\nStop the cache first and try again.");

                    serverConfig = cacheServer.GetNewConfiguration(CacheName);
                    //ConfiguredCacheInfo[] configuredCaches = cacheServer.GetAllConfiguredCaches();

                    if (serverConfig == null)
                        throw new Exception("Specified cache is not registered on the given server.");

                    //if (! Unregister)
                    //{
                    try
                    {
                        asm = AssemblyDef.LoadFrom(AssemblyPath);

                        extension = Path.GetExtension(asm.FullName);
                    }
                    catch (Exception e)
                    {
                        string message = string.Format("Could not load assembly \"" + AssemblyPath + "\". {0}", e.Message);
                        OutputProvider.WriteErrorLine("Error : {0}", message);
                        successful = false;
                        return;
                    }

                    if (asm == null)
                        throw new Exception("Could not load specified Assembly");

                    TypeDef type = asm.GetType(Class);


                    if (serverConfig.CacheSettings.QueryIndices == null)
                    {
                        serverConfig.CacheSettings.QueryIndices = new Alachisoft.NCache.Config.Dom.QueryIndex();
                        serverConfig.CacheSettings.QueryIndices.Classes = queryClasses;
                    }

                    queryClasses = serverConfig.CacheSettings.QueryIndices.Classes;

                    serverConfig.CacheSettings.QueryIndices.Classes = GetSourceClass(GetClass(queryClasses, asm));


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

                                OutputProvider.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", node.IpAddress, CacheName);
                                cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                            }
                            catch (Exception ex)
                            {
                                OutputProvider.WriteErrorLine("Failed to Add Query Index on '{0}'. ", serverName);
                                OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                                failedNodes = failedNodes + "/n" + node.IpAddress.ToString();
                                successful = false;
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
                            OutputProvider.WriteLine("Adding query indexes on node '{0}' to cache '{1}'.", serverName, CacheName);
                            cacheServer.RegisterCache(CacheName, serverConfig, "", true, userId, paswd, false);
                        }
                        catch (Exception ex)
                        {
                            OutputProvider.WriteErrorLine("Failed to Add Query Index on '{0}'. ", serverName);
                            OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);
                            successful = false;
                        }
                        finally
                        {
                            cacheServer.Dispose();
                        }
                    }

                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                successful = false;
            }
            finally
            {
                NCache.Dispose();
                if (successful && !IsUsage)
                    OutputProvider.WriteLine("Query indexes successfully added.");
            }
        }
        public Hashtable ClassToHashtable(Alachisoft.NCache.Config.Dom.Class[] cl)
        {
            Hashtable hash = new Hashtable();
            for (int i = 0; i < cl.Length; i++)
            {
                hash.Add(cl[i].Name, cl[i]);
            }
            return hash;
        }

        public Class[] GetSourceClass(Hashtable pParams)
        {
            Class[] param = new Class[pParams.Count];
            IDictionaryEnumerator enu = pParams.GetEnumerator();
            int index = 0;
            while (enu.MoveNext())
            {
                param[index] = new Class();
                param[index].Name = (string)enu.Key;
                param[index] = (Class)enu.Value;
                index++;
            }
            return param;
        }

        public Hashtable GetClass(Alachisoft.NCache.Config.Dom.Class[] cl, AssemblyDef asm)
        {
            Hashtable hash = new Hashtable();
            Hashtable latt = new Hashtable();
            Alachisoft.NCache.Config.Dom.Class c = new Alachisoft.NCache.Config.Dom.Class();
            //c.Assembly = asm.ToString(); //cg

            c.Name = Class;
            TypeDef type = asm.GetType(Class);
            string assemblySrt = null;
            assemblySrt = asm.FullName;//= c.Assembly ; //cg

            String fullVersion = String.Empty;

            if (!String.IsNullOrEmpty(assemblySrt))
            {
                String version = assemblySrt.Split(',')[1];
                fullVersion = version.Split('=')[1];
            }
            c.ID = Class;
            if (cl != null)
            {
                hash = ClassToHashtable(cl);

            }
            //defaults = CreateLuceneDefaults();
            c.LuceneSection = new LuceneAttributes();
            if (hash.Contains(c.Name))
            {
                Class existingClass = (Class)hash[c.Name];
                if (existingClass.LuceneSection != null)
                {
                    c.LuceneSection = existingClass.LuceneSection;
                }
            }
            c.LuceneSection = WriteLuceneDefaults(c.LuceneSection);

            hash[c.Name] = c;
            return hash;
        }
        private Dictionary<string,LuceneDeployment> GetDeployements(LuceneDepType depTypes)
        {
            Dictionary<string, LuceneDeployment> deployments = new Dictionary<string, LuceneDeployment>();
            if (depTypes != null)
            {
                foreach (LuceneDeployment dep in depTypes.Providers)
                {
                    deployments.Add(dep.Name, dep);
                }
            }
            return deployments;
        }
        private LuceneAttributes WriteLuceneDefaults(LuceneAttributes existing)
        {
            if(serverConfig.CacheSettings.LuceneSettings != null)
            {
                AttributeValidator.ValidateDefaultAnalyzer(Analyzer, GetDeployements(serverConfig.CacheSettings.LuceneSettings.Analyzers));
                AttributeValidator.ValidateDefaultStopWords(StopWords, GetDeployements(serverConfig.CacheSettings.LuceneSettings.StopWordFiles));
                AttributeValidator.ValidateDefaultPattern(Pattern, GetDeployements(serverConfig.CacheSettings.LuceneSettings.Patterns));
            }
            AttributeValidator.ValidateDefaultTermVector(TermVector);
            AttributeValidator.ValidateDefaultLuceneType(LuceneType);
            existing.MergeFactor = MergeFactor;
            existing.LuceneType = LuceneType;
            existing.LuceneAnalyzer = Analyzer;
            existing.TermVector = TermVector;
            existing.Pattern = Pattern;
            existing.StopWords = StopWords;
            return existing;

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
                string bin = directoryInfo.Parent.Parent.FullName;
                final = System.IO.Path.Combine(bin, "service");/// from where you neeed the assemblies
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
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
                TOOLNAME = "Add-LuceneQueryIndex Cmdlet";
                AddLuceneQueryIndexDefaults();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

    }
}
