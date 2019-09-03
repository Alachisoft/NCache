using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Bridging.Configuration;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsCommon.Set, "BridgeCacheConnection")]
    public class SetBridgeCacheConnectionBase : SetBridgeCacheConnectionParameters, IConfiguration
    {

        /// <summary> NCache service controller. </summary>
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string TOOLNAME = "SetBridgeCacheConnection Tool";


        public bool ValidateParameters()
        {

            _bridgeService.Port = Port;
            _bridgeService.ServerName = BridgeServer;

            

            if (Port == -1) _bridgeService.Port = _bridgeService.UseTcp ? BridgeConfigurationManager.NCacheTcpPort : BridgeConfigurationManager.NCacheHttpPort;

            if (string.IsNullOrEmpty(BridgeId))
            {
                OutputProvider.WriteErrorLine("Error: Bridge-id not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(CacheName))
            {
                OutputProvider.WriteErrorLine("Error: Cache-id not specified.");
                return false;
            }

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;

        }

        private bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip, out adress);
        }
        private bool VerifyBridgeMasterCache(string bridgeID, bool findConnectedMaster, BridgeConfiguration bridgeConfig)
        {

            bool isBridgeCacheMaster = false;

            if (bridgeConfig != null && bridgeConfig.TargetCacheConfigList != null)
            {
                foreach (TargetCacheCofiguration targetCacheConfig in bridgeConfig.TargetCacheConfigList)
                {
                    if (targetCacheConfig != null && targetCacheConfig.IsMaster)
                    {
                        if (findConnectedMaster && targetCacheConfig.IsConnected || !findConnectedMaster)
                        {
                            isBridgeCacheMaster = true;
                            break;
                        }
                    }
                }
            }
            return isBridgeCacheMaster;
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>
        public void SetBridgeCacheConnection()
        {

            try
            {

                _bridgeService = new NCBridgeRPCService(BridgeServer);
                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));


                if (!ValidateParameters())
                {
                    return;
                }

                //**********************************

                //cacheServer = GetCacheServers(config.Cluster.GetAllConfiguredNodes());
                bool isNotMaster = false;
                int passiveCount = 0;
                bool cacheExists = false;
                TargetCacheCofiguration targtCacheConfig = null;
                if (_bridgeServer != null)
                {
                    try
                    {
                        OutputProvider.WriteLine("Setting Cache '{0}' Connection To Bridge '{1}' on {2}:{3}.", CacheName, BridgeId, _bridgeService.ServerName, _bridgeService.Port);
                        Alachisoft.NCache.Bridging.Configuration.BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);
                        ToolsUtil.VerifyBridgeConfigurations(bridgeConfig, BridgeId);


                        List<TargetCacheCofiguration> previouslyAddedCaches = bridgeConfig.TargetCacheConfigList;
                        if (previouslyAddedCaches.Count >= 1)
                        {
                            //checking validations regarding bridge
                            foreach (TargetCacheCofiguration pCache in previouslyAddedCaches)
                            {
                                if (pCache.CacheID.ToLower().Equals(CacheName.ToLower()))
                                {
                                    
                                        cacheExists = true;
                                        isNotMaster = !pCache.IsMaster;
                                        targtCacheConfig = pCache;
                                    
                                   

                                }
                                if (pCache.Status.Equals("passive"))
                                {
                                    passiveCount++;
                                }

                            }
                        }
                        else
                        {
                            OutputProvider.WriteErrorLine("No Cache Exists in Bridge {0}", BridgeId);
                            return;
                        }
                        if( previouslyAddedCaches.Count==2 && !isNotMaster && cacheExists && Connection.ToString().ToLower().Equals("disconnect") )
                        {
                            OutputProvider.WriteErrorLine("Master Cache '{0}'cannot be disconnected,please change the master cache First", CacheName);
                            return;

                        }
                        
                        if(!cacheExists)
                        {
                            OutputProvider.WriteErrorLine("No Cache Exists in Bridge {0} with CacheId {1}", BridgeId, CacheName);
                        }


                        WriteToBridgeConf(bridgeConfig, targtCacheConfig);
                        OutputProvider.WriteLine("Bridge Connection for Bridge {0} set to {1} Successfully!",BridgeId,Connection.ToString());



                    }
                    catch (SecurityException e)
                    {
                        OutputProvider.WriteErrorLine("Failed to Set Cache Connection Status to bridge '{0}'. Error: {1} ", BridgeId, e.Message);
                    }
                    catch (Exception e)
                    {
                        OutputProvider.WriteErrorLine("Failed to Set Cache Connection Status to bridge '{0}'. Error: {1} ", BridgeId, e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error: {0}", e.Message);
            }
            finally
            {

                if (_bridgeService != null) _bridgeService.Dispose();
               

            }

        }

        public void WriteToBridgeConf(Alachisoft.NCache.Bridging.Configuration.BridgeConfiguration bridgeConfig,
            TargetCacheCofiguration targtCacheConfig 
            )
        {

            //Adding Bridge to bridge.ncconf 
            bridgeConfig.TargetCacheConfigList.Remove(targtCacheConfig);
            targtCacheConfig.IsConnected = (Connection.ToString().ToLower() == "connect") ? true : false;

            bridgeConfig.TargetCacheConfigList.Add(targtCacheConfig);

            // write in all bridge nodes bridge.nconnf file
            char[] separater = { ',' };
            bridgeConfig.DeploymentVersion++;
            foreach (string bridgeIp in bridgeConfig.BridgeNodes.Split(separater).ToList())
            {
                _bridgeService = new NCBridgeRPCService(bridgeIp);
                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));

                _bridgeServer.RegisterBridge(bridgeConfig, true, true);

            }


        }
        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        protected override void BeginProcessing()
        {
            try
            {

                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Set-BridgeCacheConnection Cmdlet";
                SetBridgeCacheConnection();
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
