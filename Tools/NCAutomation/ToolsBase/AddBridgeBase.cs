using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Bridging.Configuration;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System.Reflection;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.New, "Bridge")]
    public class AddBridgeBase : AddBridgeParameters, IConfiguration
    {
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string TOOLNAME = "AddBridge Tool";
        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        public bool ValidateParameters()
        {
            

            //validate BridgeID
            if (string.IsNullOrEmpty(BridgeId))
            {
                OutputProvider.WriteErrorLine("Error: BridgeID not specified.");
                return false;
            }

            //validate Active Node
            if (string.IsNullOrEmpty(ActiveNode))
            {
                OutputProvider.WriteErrorLine("Error: Bridge Active Node not specified.");
                return false;
            }
            
            
            ToolsUtil.PrintLogo(OutputProvider, printLogo,TOOLNAME);
            return true;
        }

        public void AddBridge()
        {
            try
            {
               
                if (!ValidateParameters())
                    return;

                _bridgeService = new NCBridgeRPCService(ActiveNode);              

                _bridgeService.ServerName = ActiveNode;

                if (Port == -1)
                    _bridgeService.Port = _bridgeService.UseTcp ? BridgeConfigurationManager.NCacheTcpPort : BridgeConfigurationManager.NCacheHttpPort;
                else
                    _bridgeService.Port = Port;

                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));



                BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);
                if (bridgeConfig != null)
                {
                    OutputProvider.WriteErrorLine("Error: Bridge with specified Bridge Id already exists.");
                    return;
                }

                else
                {
                    bridgeConfig = new BridgeConfiguration();
                    bridgeConfig.ConfigID = Guid.NewGuid().ToString();
                }


                bridgeConfig.ID = BridgeId;
               
                bridgeConfig.ReplicatorVirtualQueueSize = ReplicatorQueueSize;
                bridgeConfig.QueueConfig.Size = MaxQueueSize;
                bridgeConfig.QueueConfig.OptimizationEnabled = QueueOptimized;
                bridgeConfig.BridgeActive = ActiveNode;
                bridgeConfig.BridgeNodes = ActiveNode;



                if (!string.IsNullOrEmpty(PassiveNode))
                {
                    bridgeConfig.BridgeNodes = bridgeConfig.BridgeNodes + "," + PassiveNode;
                    //specify port for bridge
                    if (BridgePort != -1)
                    {
                        if (IsBridgePortAvailable(bridgeConfig, BridgePort))
                        {
                            bridgeConfig.BridgePort = BridgePort;
                        }

                        else
                        {
                            OutputProvider.WriteErrorLine("Error: Specified bridge port is not available.");
                            return;
                        }
                    }

                    else
                    {
                        bridgeConfig.BridgePort = NextAvailableBridgePort(bridgeConfig);
                    }

                    try
                    {
                        NCBridgeRPCService _bridgePassiveNodeService = new NCBridgeRPCService(PassiveNode);
                        IBridgeServer _bridgePassiveServer = _bridgePassiveNodeService.GetBridgeServer(TimeSpan.FromSeconds(30));
                        _bridgePassiveServer.RegisterBridge(bridgeConfig, false, false);
                    }
                    catch (Exception ee)
                    {
                        
                        OutputProvider.WriteErrorLine("Error: Bridge could not be added on passive node.");
                        return;
                    }
                }

                else
                {
                    //specify port for bridge
                    if (BridgePort != -1)
                    {
                        if (IsBridgePortAvailable(bridgeConfig, BridgePort))
                        {
                            bridgeConfig.BridgePort = BridgePort;
                        }

                        else
                        {
                            OutputProvider.WriteErrorLine("Error: Specified bridge port is not available.");
                            return;
                        }
                    }

                    else
                    {
                        bridgeConfig.BridgePort = NextAvailableBridgePort(bridgeConfig);
                    }
                }

                try
                {
                    _bridgeServer.RegisterBridge(bridgeConfig, false, false);
                    OutputProvider.WriteLine("Bridge succesfully added.");
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: Bridge could not be added.");
                    return;
                }
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine("Error: " + ex.Message);
            }

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
                TOOLNAME = "New-Bridge Cmdlet";
                AddBridge();
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

        private int NextAvailableBridgePort(BridgeConfiguration bconfig)
        {


            ArrayList nodes = bconfig.GetBridgeNodeList();
            string serverName = "";
            int nextPort = 10000;
            Hashtable bridgeProps = new Hashtable();
            NCBridgeRPCService sw;
            try
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    serverName = nodes[i].ToString();
                    sw = new NCBridgeRPCService(serverName);
                    IBridgeServer server = sw.GetBridgeServer(TimeSpan.FromSeconds(30));
                    BridgeConfiguration[] configs = server.GetAllBridgesConfiguration();
                    Hashtable bridgeList = new Hashtable();

                    if (configs != null)
                    {
                        for (int j = 0; j < configs.Length; j++)
                        {
                            bridgeList.Add(configs[j].ID.ToLower(), configs[j]);
                        }
                    }

                    bridgeProps = bridgeList;

                    if (bridgeProps != null)
                    {
                        IDictionaryEnumerator ide = bridgeProps.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            try
                            {
                                string bridgeName = ide.Key as string;
                                BridgeConfiguration bridgeConfig = new BridgeConfiguration();
                                if (ide.Value is BridgeConfiguration)
                                {
                                    bridgeConfig = (BridgeConfiguration)ide.Value;
                                }

                                if (bridgeConfig != null && bridgeConfig.BridgePort > nextPort)
                                {
                                    nextPort = bridgeConfig.BridgePort;
                                }
                            }
                            catch (Exception e)
                            {
                                OutputProvider.WriteErrorLine("Error: " + e.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine("Error: Failed to fetch next bridge availabe port from " + serverName + "\n\nError: " + ex.Message);
            }


            return ++nextPort;
        }

        private bool IsBridgePortAvailable(BridgeConfiguration bconfig,int newBridgePort)
        {
            ArrayList nodes = bconfig.GetBridgeNodeList();
            string serverName = "";
            Hashtable bridgeProps = new Hashtable();
            NCBridgeRPCService sw;
            bool isAvailable = true;
            try
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    serverName = nodes[i].ToString();
                    sw = new NCBridgeRPCService(serverName);
                    IBridgeServer server = sw.GetBridgeServer(TimeSpan.FromSeconds(30));
                    BridgeConfiguration[] configs = server.GetAllBridgesConfiguration();
                    Hashtable bridgeList = new Hashtable();

                    if (configs != null)
                    {
                        for (int j = 0; j < configs.Length; j++)
                        {
                            bridgeList.Add(configs[j].ID.ToLower(), configs[j]);
                        }
                    }

                    bridgeProps = bridgeList;
                    if (bridgeProps != null)
                    {
                        IDictionaryEnumerator ide = bridgeProps.GetEnumerator();
                        while (ide.MoveNext())
                        {
                            try
                            {
                                string bridgeName = ide.Key as string;
                                BridgeConfiguration bridgeConfig = new BridgeConfiguration();

                                if (ide.Value is BridgeConfiguration)
                                {
                                    bridgeConfig = (BridgeConfiguration)ide.Value;
                                }

                                if (bridgeConfig != null && bridgeConfig.BridgePort == newBridgePort)
                                {
                                    isAvailable = false;
                                    break;
                                }
                            }
                            catch (Exception e)
                            {
                                OutputProvider.WriteErrorLine("Error: " + e.Message);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               OutputProvider.WriteErrorLine("Error: Failed to fetch bridge port info from " + serverName + "\n\nError: " + ex.Message);
            }
            return isAvailable;
        }
    }
}
