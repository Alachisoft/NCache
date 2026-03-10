
using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Management.Automation;
using Alachisoft.NCache.Config.NewDom;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsLifecycle.Start, "Cache")]
    public class StartCacheBase : StartCacheParameters, IConfiguration
    {
        static private string _partId = string.Empty;
        private string TOOLNAME = "StartCache Tool";
        NCacheRPCService NCache = new NCacheRPCService("");
        private string[] servers;
        void ApplyParameters()
        {

            if (Name != null && Name.Length > 0)
            {
                CachesList = new ArrayList();
                CachesList.AddRange(Name);
            }
            if (String.IsNullOrEmpty(Server))
            {
                servers = new string[1];
                servers[0] = System.Environment.MachineName;

            }
            else
            {
                servers = Server.Split(new char[] { ',' });
            }
            if (String.IsNullOrEmpty(NCache.ServerName))
                NCache.ServerName = System.Environment.MachineName;
            NCache.Port = Port;
            if (NCache.Port == -1)
            {
                NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            }
            if (CachesList.Count == 0)
            {
                OutputProvider.WriteErrorLine("Error: cache name not specified.");
            }
            if (CachesList.Count > 1)
                _partId = string.Empty;
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);


        }

        void StartCacheOnServer()
        {
            string cacheIp = string.Empty;
            ICacheServer cacheServer = null;
            try
            {
                foreach (string server in servers)
                {
                    NCache.ServerName = server;
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    CacheServerConfig config = null;
                    if (cacheServer != null)
                    {
                        cacheIp = cacheServer.GetClusterIP();
                        foreach (string cache in CachesList)
                        {

                            try
                            {
                                config = cacheServer.GetNewConfiguration(cache);
                                if (config !=null && config.CacheSettings.CacheTopology.Topology != "local-cache")
                                {
                                    var serverNodes = config.CacheDeployment.Servers.ServerNodeList;
                                    if (serverNodes.Length > 3)
                                    {
                                        throw new Exception("Open Source edition of NCache cannot have a cache cluster of more than 3 nodes.");
                                    }
                                }

                                if (config != null && config.CacheSettings.InProc)
                                {
                                    throw new Exception("InProc caches cannot be started explicitly.");
                                }

                                OutputProvider.WriteLine("Starting cache '{0}' on server {1}:{2}.", cache, cacheIp, NCache.Port);

                                cacheServer.StartCache(cache, _partId);


                                OutputProvider.WriteLine("'{0}' successfully started on server {1}:{2}. \n", cache, cacheIp,
                                 NCache.Port);
                            }
                            catch (SecurityException e)
                            {
                                OutputProvider.WriteErrorLine("Failed to start '{0}' on server {1}.", cache,
                                      cacheIp);
                                OutputProvider.WriteErrorLine(e.Message);

                            }
                            catch (Exception e)
                            {
                                OutputProvider.WriteErrorLine(
                                "Failed to start '{0}' on server {1}. Error: {2}",
                                cache,
                                cacheIp,
                                e.Message
                            );

                            }
                            finally
                            {
                                if (cacheServer != null)
                                    cacheServer.Dispose();
                            }
                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                OutputProvider.WriteErrorLine("Error : {0}", "NCache service could not be contacted on server");

                OutputProvider.WriteErrorLine(ex.ToString());
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                OutputProvider.WriteErrorLine(e.ToString());
            }
            finally
            {
                NCache.Dispose();
            }

        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        public void StartCacheProcess()
        {
            try
            {
                ApplyParameters();
                StartCacheOnServer();
            }
            catch
            {
                throw;
            }
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Start-Cache Cmdlet";
                StartCacheProcess();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }

        public bool ValidateParameters()
        {
            return false;
        }
        
    }
}