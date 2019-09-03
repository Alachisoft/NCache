using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Bridging.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.RPC;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Reflection;
using System.IO;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Add, "BridgeNode")]
    public class AddBridgeNodeBase : AddBridgeNodeParameters, IConfiguration
    {
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string TOOLNAME = "AddBridgeNode Tool";

        public bool ValidateParameters()
        {
            _bridgeService.Port = Port;
            _bridgeService.ServerName = Server;
            if (Port == -1) _bridgeService.Port = _bridgeService.UseTcp ? BridgeConfigurationManager.NCacheTcpPort : BridgeConfigurationManager.NCacheHttpPort;
            //validate BridgeID
            if (string.IsNullOrEmpty(BridgeId))
            {
                OutputProvider.WriteErrorLine("Error: BridgeID not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(Server))
            {
                OutputProvider.WriteErrorLine("Error: Bridge server is not specified.");
                return false;
            }

            //validate Server
            if (string.IsNullOrEmpty(NewBridgeNode))
            {
                OutputProvider.WriteErrorLine("Error: New server is not specified.");
                return false;
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);

            return true;
        }

        public void AddBridgeNode()
        {
            try
            {
                            
                _bridgeService = new NCBridgeRPCService(Server);

                if (!ValidateParameters())
                    return;

                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));
               

                BridgeConfiguration bconfig = _bridgeServer.GetBridgeConfiguration(BridgeId);

                ToolsUtil.VerifyBridgeConfigurations(bconfig, BridgeId);       
                        
                byte[] userId = null;
                byte[] paswd = null;
                if (UserId != string.Empty && Password != string.Empty)
                {
                    userId = EncryptionUtil.Encrypt(UserId);
                    paswd = EncryptionUtil.Encrypt(Password);
                }

                if (bconfig != null && bconfig.NumberOfBridgeNodes >= 2)
                {
                    OutputProvider.WriteErrorLine("You cannot add more than 2 nodes in a Bridge");
                    return;
                }
                

                if (!bconfig.GetBridgeNodeList().Contains(NewBridgeNode))
                {
                    
                    if (bconfig.GetBridgeNodeList().Count != 0)
                    {
                        bconfig.DeploymentVersion++;
                        bconfig.BridgeNodes = bconfig.BridgeNodes + "," + NewBridgeNode;
                        _bridgeServer.RegisterBridge(bconfig, true, false);
                        //for registering bridge on newly added node
                        NCBridgeRPCService _bridgeNewService = new NCBridgeRPCService(NewBridgeNode);
                        IBridgeServer _bridgeNewServer = _bridgeNewService.GetBridgeServer(TimeSpan.FromSeconds(30));
                        _bridgeNewServer.RegisterBridge(bconfig, true, false);
                        OutputProvider.WriteLine(NewBridgeNode + " added to " + BridgeId);
                    }

                    else
                    {
                        bconfig.BridgeNodes = NewBridgeNode;
                        bconfig.BridgeActive = NewBridgeNode;
                        NCBridgeRPCService _bridgeNewService = new NCBridgeRPCService(NewBridgeNode);
                        IBridgeServer _bridgeNewServer = _bridgeNewService.GetBridgeServer(TimeSpan.FromSeconds(30));
                        _bridgeNewServer.RegisterBridge(bconfig, true, false);
                        OutputProvider.WriteLine(NewBridgeNode + " added to " + BridgeId);
                        
                    }

                    //updating target cache config
                    if (bconfig.TargetCacheConfigList != null)
                    {
                        foreach (TargetCacheCofiguration targetCacheConfig in bconfig.TargetCacheConfigList)
                        {
                            try
                            {
                                if (targetCacheConfig != null)
                                {
                                    foreach (string server in targetCacheConfig.Servers.Split(','))
                                    {
                                        NCacheRPCService NCache = new NCacheRPCService(server); ;
                                        ICacheServer cacheServer = null;
                                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                        string cacheID = targetCacheConfig.CacheID.ToString();

                                        CacheServerConfig serverConfig = cacheServer.GetCacheConfiguration(cacheID);
                                        if (serverConfig.Bridge != null)
                                        {
                                            serverConfig.Bridge.Servers = bconfig.BridgeNodes;
                                        }

                                        serverConfig.ConfigVersion++;

                                        cacheServer.ConfigureBridgeToCache(serverConfig, userId, paswd, false);
                                    }



                                }
                            }
                            catch (Exception e)
                            {
                                OutputProvider.WriteErrorLine(e.Message);
                                return;
                            }

                            
                        }
                    }

                }
                else
                {
                   
                    OutputProvider.WriteErrorLine("Error: Node already exists in bridge");
                    return;
                }
            }
            catch(Exception e)
            {
                OutputProvider.WriteErrorLine("Error: Bridge not registered on server.\n");
                OutputProvider.WriteErrorLine(e.Message);
            }

          
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
                TOOLNAME = "Add-BridgeNode Cmdlet";
                AddBridgeNode();
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
    }
}
