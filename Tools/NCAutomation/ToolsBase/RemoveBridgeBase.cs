using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Bridging.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Remove, "Bridge")]
    public class RemoveBridgeBase : RemoveBridgeParameters, IConfiguration
    {
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string TOOLNAME = "RemoveBridge Tool";
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
            if (string.IsNullOrEmpty(Server))
            {
                OutputProvider.WriteErrorLine("Error: Bridge server is not specified.");
                return false;
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        public void RemoveBridge()
        {
           
            try
            {
                if (!ValidateParameters())
                    return;

                _bridgeService = new NCBridgeRPCService(Server);
                if (Port == -1) _bridgeService.Port = _bridgeService.UseTcp ? BridgeConfigurationManager.NCacheTcpPort : BridgeConfigurationManager.NCacheHttpPort;
                else
                    _bridgeService.Port = Port;

                _bridgeService.ServerName = Server;


                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));

                
                BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);

                byte[] userId = null;
                byte[] paswd = null;
                if (UserId != string.Empty && Password != string.Empty)
                {
                    userId = EncryptionUtil.Encrypt(UserId);
                    paswd = EncryptionUtil.Encrypt(Password);
                }

                if (bridgeConfig.GetBridgeNodeList().Count == 2)
                {

                    _bridgeServer.UnRegisterBridge(BridgeId);
                    if (bridgeConfig.BridgeNodes.Contains(_bridgeServer + ","))
                        bridgeConfig.BridgeNodes = bridgeConfig.BridgeNodes.Replace(_bridgeServer + ",", "");
                    else
                        bridgeConfig.BridgeNodes = bridgeConfig.BridgeNodes.Replace("," + _bridgeServer, "");

                    bridgeConfig.BridgeActive = bridgeConfig.BridgeNodes;
                    NCBridgeRPCService _bridge = new NCBridgeRPCService(bridgeConfig.BridgeActive);
                    IBridgeServer _server = _bridge.GetBridgeServer(TimeSpan.FromSeconds(30));
                    _server.UnRegisterBridge(BridgeId);
                }
                else
                    _bridgeServer.UnRegisterBridge(BridgeId);

                bridgeConfig.BridgeNodes = "";

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
                                        serverConfig.Bridge = null;
                                    }

                                    serverConfig.ConfigVersion++;
                                    cacheServer.ConfigureBridgeToCache(serverConfig, userId, paswd, false);
                                }

                            }

                          }
                        
                
                        catch (Exception ex)
                        {
                            OutputProvider.WriteErrorLine(ex.Message);
                            return;
                        }
                    }
                }

                OutputProvider.WriteLine("Bridge removed.");
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Bridge not registered on server.\n");
                OutputProvider.WriteErrorLine("Error: " + e.Message);
                
                return;
            }
        }

     

        protected override void BeginProcessing()
        {
            try
            {
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Remove-Bridge Cmdlet";
                RemoveBridge();
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
