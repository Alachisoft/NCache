using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Bridging.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
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
    [Cmdlet(VerbsCommon.Remove, "BridgeNode")]
    public class RemoveBridgeNodeBase : RemoveBridgeNodeParameters, IConfiguration
    {
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string TOOLNAME = "RemoveBridgeNode Tool";
        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

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
            if (string.IsNullOrEmpty(DeleteBridgeNode))
            {
                OutputProvider.WriteErrorLine("Error: Server to be removed is not specified.");
                return false;
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        public void RemoveBridgeNode()
        {
            try
            {
                _bridgeService = new NCBridgeRPCService(Server);
                if (!ValidateParameters())
                    return;

                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));


                BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);

                if(!ToolsUtil.VerifyBridgeConfigurations(bridgeConfig, BridgeId))
                {
                    OutputProvider.WriteErrorLine("Error: Configuration mismatch.");
                    return;
                }
                
                byte[] userId = null;
                byte[] paswd = null;
                if (UserId != string.Empty && Password != string.Empty)
                {
                    userId = EncryptionUtil.Encrypt(UserId);
                    paswd = EncryptionUtil.Encrypt(Password);
                }

                if (DeleteBridgeNode == Server)
                {
                    _bridgeServer.UnRegisterBridge(BridgeId);
                }
                else
                {
                    NCBridgeRPCService _removeBridgeService = new NCBridgeRPCService(DeleteBridgeNode);
                    IBridgeServer _removeServer = _removeBridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));
                    _removeServer.UnRegisterBridge(BridgeId);
                }

                if (bridgeConfig != null)
                {
                    if (bridgeConfig.GetBridgeNodeList().Count == 1)
                        bridgeConfig.BridgeNodes = "";

                    else if (bridgeConfig.BridgeNodes.Contains(DeleteBridgeNode + ","))
                        bridgeConfig.BridgeNodes = bridgeConfig.BridgeNodes.Replace(DeleteBridgeNode + ",", "");
                    else
                        bridgeConfig.BridgeNodes = bridgeConfig.BridgeNodes.Replace("," + DeleteBridgeNode, "");

                    if (bridgeConfig.GetBridgeNodeList().Count == 0)
                        bridgeConfig.BridgeActive = string.Empty;
                    else
                        bridgeConfig.BridgeActive = bridgeConfig.BridgeNodes;
                    bridgeConfig.DeploymentVersion++;
                    if (bridgeConfig.GetBridgeNodeList().Count > 0 && !string.IsNullOrEmpty(bridgeConfig.BridgeNodes))
                    {
                        NCBridgeRPCService _bridge = new NCBridgeRPCService(bridgeConfig.BridgeActive);
                        IBridgeServer _server = _bridge.GetBridgeServer(TimeSpan.FromSeconds(30));
                        _server.RegisterBridge(bridgeConfig, true, false);
                    }

                    //updating target cache config
                    if (bridgeConfig.TargetCacheConfigList != null)
                    {
                        foreach (TargetCacheCofiguration targetCacheConfig in bridgeConfig.TargetCacheConfigList)
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
                                            serverConfig.Bridge.Servers = bridgeConfig.BridgeNodes;
                                        }
                                        serverConfig.ConfigVersion++;
                                        cacheServer.ConfigureBridgeToCache(serverConfig, userId, paswd, false);


                                    }
                                }
                                
                            }
                            catch(Exception ex)
                            {
                                OutputProvider.WriteErrorLine(ex.Message);
                                
                                return;
                            }
                        }
                    }
                }
                OutputProvider.WriteLine("Bridge node" + DeleteBridgeNode.ToString() + " removed successfully from Bridge " + BridgeId);
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Bridge not registered on server.\n");
                OutputProvider.WriteErrorLine(e.Message);

                return;
            }
        }



        protected override void BeginProcessing()
        {
            try
            {
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Remove-BridgeNode Cmdlet";
                RemoveBridgeNode();
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
