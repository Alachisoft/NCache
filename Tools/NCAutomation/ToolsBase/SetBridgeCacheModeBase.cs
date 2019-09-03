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
    [Cmdlet(VerbsCommon.Set, "BridgeCacheMode")]
    public class SetBridgeCacheModeBase : SetBridgeCacheModeParameters, IConfiguration
    {

        /// <summary> NCache service controller. </summary>
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string[] _serverList = null;

        private NCacheRPCService NCache = new NCacheRPCService("");
        //private Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig;
        private Alachisoft.NCache.Config.Dom.CacheServerConfig config = null;

        private ICacheServer cacheServer;
        private List<string> serverNodes = new List<string>();
        private string TOOLNAME = "SetBridgeCacheMode Tool";

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


            bool multipleServers = false;
            if (CacheServer != null || CacheServer != string.Empty)
            {
                _serverList = CacheServer.Split(',');
                if (_serverList.Length > 1 || (_serverList[0].Contains(":")))
                {
                    multipleServers = true;
                }
            }
            VerifyCacheServer(multipleServers);

            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;

        }

        private bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip, out adress);
        }
        public void VerifyCacheServer(bool multipleServers)
        {
            int index = 0;
            string server = "";

        tryNextServer:
            if (multipleServers)
            {
                string[] serverAddress = _serverList[index].Split(':');
                if (serverAddress.Length == 2)
                {
                    if (!IsValidIP(serverAddress[0]))
                    {
                        OutputProvider.WriteErrorLine("Error: Invalid Server IP.");
                        throw new Exception("Invalid IP :" + serverAddress[0]);

                    }
                    NCache.ServerName = serverAddress[0];
                    try
                    {
                        NCache.Port = Convert.ToInt32(serverAddress[1]);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid Port :" + serverAddress[1] + "specified for server : " + serverAddress[0] + ".");
                    }

                }
                else
                    if (serverAddress.Length == 1)
                    {
                        if (!IsValidIP(serverAddress[0]))
                        {
                            OutputProvider.WriteErrorLine("Error: Invalid Server IP.");
                            throw new Exception("Invalid Server IP :" + serverAddress[0] + ".");
                        }
                        NCache.ServerName = serverAddress[0];
                        NCache.Port = Port;
                        if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                    }
                    else
                    {
                        throw new Exception("Invalid server Address specified, kindly specify as [IPAdress] or [IPAdress]:[Port].");
                    }
            }
            else
            {
                if (Port != -1)
                {
                    NCache.Port = Port;
                }
                if (CacheServer != null || CacheServer != string.Empty)
                {
                    NCache.ServerName = CacheServer;
                }
                if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            }
            try
            {
                server = NCache.ServerName;
                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
            }
            catch (Exception ex)
            {
                if (multipleServers)
                {
                    if (index < _serverList.Length - 1)
                    {
                        OutputProvider.WriteErrorLine("Failed to connect to server : " + NCache.ServerName + ":" + NCache.Port + " \nTrying next server.");
                        index++;
                        goto tryNextServer;
                    }
                }
                throw ex;
            }

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
        public void SetBridgeCacheMode()
        {

            try
            {

                _bridgeService = new NCBridgeRPCService(BridgeServer);
                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));

                NCacheRPCService nService = new NCacheRPCService(CacheServer);
                cacheServer = nService.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                if (!ValidateParameters())
                {
                    return;
                }
                
                //**********************************
                config = cacheServer.GetCacheConfiguration(CacheName);

                if (config == null)
                {
                    OutputProvider.WriteErrorLine("Error : The cache'{0}' does not exist on server {1}:{2} .", CacheName, NCache.ServerName, NCache.Port);
                    return;
                }

                if (config.CacheType.ToLower().Equals("local-cache"))
                {
                    OutputProvider.WriteLine("Local Cache cannot be Set as a bridge cache");
                    return;
                }
                //cacheServer = GetCacheServers(config.Cluster.GetAllConfiguredNodes());
                bool isNotMaster = false;
                int passiveCount = 0;
                
                TargetCacheCofiguration targtCacheConfig = null;
                if (_bridgeServer != null)
                {
                    try
                    {
                        OutputProvider.WriteLine("Setting Cache Mode To Bridge '{0}' on {1}:{2}.", BridgeId, _bridgeService.ServerName, _bridgeService.Port);
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
                                        isNotMaster =!pCache.IsMaster;
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
                        }

                        if (passiveCount >= 1 && State.Equals(BridgeCacheStateParam.Passive))
                        {
                            OutputProvider.WriteErrorLine("There is already a Passive cache in Bridge", BridgeId);
                            return;

                        }
                        if (!isNotMaster)
                        {
                            OutputProvider.WriteErrorLine("Bridge Cache '{0}' is a master Node,please change masterNode first", CacheName);
                            return;
                        }
                        if(targtCacheConfig.Status.Equals(State.ToString()))
                        {
                            OutputProvider.WriteErrorLine("Bridge Cache '{0}' is already {1}", CacheName ,State.ToString());
                            return;

                        }

                        //   

                        //Adding Bridge to config.ncconf 
                        BridgeConfig bridgeConf = config.Bridge;                        
                        bridgeConf.Status = State.ToString();

                        config.Bridge = bridgeConf;
                        WriteToClientConf();

                        WriteToBridgeConf(bridgeConfig, targtCacheConfig);

                        OutputProvider.WriteLine("Cache '{0}' mode set to {1}", BridgeId, State.ToString());
                    }
                    catch (SecurityException e)
                    {
                        OutputProvider.WriteErrorLine("Failed to Add Cache to bridge '{0}'. Error: {1} ", BridgeId, e.Message);
                        
                    }
                    catch (Exception e)
                    {
                        OutputProvider.WriteErrorLine("Failed to Add Cache to bridge '{0}'. Error: {1} ", BridgeId, e.Message);
                       
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
                if (NCache != null) NCache.Dispose();

            }

        }
        public void WriteToBridgeConf(Alachisoft.NCache.Bridging.Configuration.BridgeConfiguration bridgeConfig,
            TargetCacheCofiguration targtCacheConfig
            )
        {
            char[] separater = { ',' };

            bridgeConfig.TargetCacheConfigList.Remove(targtCacheConfig);
            targtCacheConfig.Status = State.ToString();

            bridgeConfig.TargetCacheConfigList.Add(targtCacheConfig);
            // write in all bridge nodes bridge.nconnf file
            bridgeConfig.DeploymentVersion++;
            foreach (string bridgeIp in bridgeConfig.BridgeNodes.Split(separater).ToList())
            {
                _bridgeService = new NCBridgeRPCService(bridgeIp);
                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));

                _bridgeServer.RegisterBridge(bridgeConfig, true, true);

            }

        }
        private void WriteToClientConf()
        {
            byte[] userId = null;
            byte[] paswd = null;
            if (UserId != string.Empty && Password != string.Empty)
            {
                userId = EncryptionUtil.Encrypt(UserId);
                paswd = EncryptionUtil.Encrypt(Password);
            }

            //writing to config.ncconf
            config.ConfigVersion++;
            cacheServer.ConfigureBridgeToCache(config, userId, paswd, true);
            cacheServer.HotApplyBridgeReplicator(CacheName, false);

            ConveyToRegisteredNodes();

        }
        private void ConveyToRegisteredNodes()
        {
            byte[] userId = null;
            byte[] paswd = null;
            if (UserId != string.Empty && Password != string.Empty)
            {
                userId = EncryptionUtil.Encrypt(UserId);
                paswd = EncryptionUtil.Encrypt(Password);
            }


            Alachisoft.NCache.Bridging.Configuration.BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);
            List<TargetCacheCofiguration> previouslyAddedCaches = bridgeConfig.TargetCacheConfigList;
            char[] separater = { ',' };
            foreach (TargetCacheCofiguration pCache in previouslyAddedCaches)
            {
                //if exists than remove
                foreach (string server in pCache.Servers.Split(separater).ToList())
                {
                    NCacheRPCService nNCache = new NCacheRPCService(server);
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    config.ConfigVersion++;
                    cacheServer.ConfigureBridgeToCache(config, userId, paswd, true);
                    cacheServer.HotApplyBridgeReplicator(CacheName, false);

                }

            }

            /*            byte[] userId = null;
                        byte[] paswd = null;
                        if (UserId != string.Empty && Password != string.Empty)
                        {
                            userId = EncryptionUtil.Encrypt(UserId);
                            paswd = EncryptionUtil.Encrypt(Password);
                        }


                        string pId="";
                        NewCacheRegisterationInfo info = cacheServer.GetNewUpdatedCacheConfiguration(CacheId.ToLower(), pId, CacheServer, true);
                        // Now update the cache configurations on all the servers where the cache
                        //is registered...
                        IPAddress address;
                        string clusterIp;
                        bool reregister=false;
                        foreach (string serverName in info.AffectedNodes)
                        {
                            if (info.AffectedPartitions.Count > 0)
                            {
                                foreach (string partId in info.AffectedPartitions)
                                {
                                    try
                                    {
                                        NCache = new NCacheRPCService(serverName);
                                        NCache.ServerName = serverName;
                                        if (!IPAddress.TryParse(NCache.ServerName, out address))
                                        {
                                            clusterIp = cacheServer.GetClusterIP();
                                            if (clusterIp != null && clusterIp != string.Empty)
                                                NCache.ServerName = clusterIp;
                                        }
                                        reregister = true;
                                        cacheServer= NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                        cacheServer.ConfigureBridgeToCache(config, userId, paswd, true);
                                        cacheServer.HotApplyBridgeReplicator(CacheId, false);

                                    }
                                    catch (Exception ex)
                                    {
                                        OutputProvider.WriteErrorLine("Failed to Create Cache on '{0}'. ", NCache.ServerName);
                                        OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);

                                        ToolsUtil.LogEvent(ex.Message, "Add Bridge Cache Base");
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
                                    NCache.ServerName = serverName;
                                    if (!IPAddress.TryParse(NCache.ServerName, out address))
                                    {
                                        clusterIp = cacheServer.GetClusterIP();
                                        if (clusterIp != null && clusterIp != string.Empty)
                                            NCache.ServerName = clusterIp;
                                    }
                                    reregister = true;
                                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                    cacheServer.ConfigureBridgeToCache(config, userId, paswd, true);
                                    cacheServer.HotApplyBridgeReplicator(CacheId, false);

                                }
                                catch (Exception ex)
                                {
                                    OutputProvider.WriteErrorLine("Failed to Create Cache on '{0}'. ", NCache.ServerName);
                                    OutputProvider.WriteErrorLine("Error Detail: '{0}'. ", ex.Message);

                                    ToolsUtil.LogEvent(ex.Message, "BridgeCacheBase");
                                    NCache.Dispose();
                                    return;
                                }
                                finally
                                {
                                    cacheServer.Dispose();
                                }
                            }
                        }
                        */
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
                TOOLNAME = "Set-BridgeCacheMode Cmdlet";
                SetBridgeCacheMode();
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
