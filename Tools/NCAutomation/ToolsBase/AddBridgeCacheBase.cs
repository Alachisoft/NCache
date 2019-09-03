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
using System.Reflection;
using System.IO;
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
    [Cmdlet(VerbsCommon.Add, "BridgeCache")]
    public class AddBridgeCacheBase : AddBridgeCacheParameters, IConfiguration
    {

        /// <summary> NCache service controller. </summary>
        private NBridgeService _bridgeService;
        private IBridgeServer _bridgeServer;
        private string[] _serverList = null;
        private string TOOLNAME = "AddBridgeCache Tool";
        private NCacheRPCService NCache = new NCacheRPCService("");
        //private Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig;
        private Alachisoft.NCache.Config.Dom.CacheServerConfig config = null;

        private ICacheServer cacheServer;
        private List<string> serverNodes = new List<string>();


        public bool ValidateParameters()
        {

                 
                       
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

            ToolsUtil.PrintLogo(OutputProvider, printLogo,TOOLNAME);
            return true;

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
                    if (!ToolsUtil.IsValidIP(serverAddress[0]))
                    {
                        OutputProvider.WriteErrorLine("Error: Invalid Server IP.");
                        throw new Exception("Invalid IP :" + serverAddress[0] );

                    }
                    NCache.ServerName = serverAddress[0];
                    try
                    {
                        NCache.Port = Convert.ToInt32(serverAddress[1]);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Invalid Port :" + serverAddress[1] + "specified for server : " + serverAddress[0] + ".");
                    }

                }
                else
                    if (serverAddress.Length == 1)
                    {
                        if (!ToolsUtil.IsValidIP(serverAddress[0]))
                        {
                            OutputProvider.WriteErrorLine("Error: Invalid Server IP.");
                            throw new Exception("Invalid Server IP :" + serverAddress[0]  + ".");
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
                cacheServer=NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
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
        public void AddBridgeCache()
        {

            try
            {
                if (!ValidateParameters())
                {
                    return;
                }
               
                _bridgeService = new NCBridgeRPCService(BridgeServer);

                if (Port == -1) _bridgeService.Port = _bridgeService.UseTcp ? BridgeConfigurationManager.NCacheTcpPort : BridgeConfigurationManager.NCacheHttpPort;
                else
                    _bridgeService.Port = Port;

                if (!string.IsNullOrEmpty(BridgeServer))
                    _bridgeService.ServerName = BridgeServer;

                _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));

                NCacheRPCService nService = new NCacheRPCService(CacheServer);

                cacheServer=nService.GetCacheServer(new TimeSpan(0,0,0,30 ));

                          

                //**********************************
                config = cacheServer.GetCacheConfiguration(CacheName);

                if (config == null)
                {
                    OutputProvider.WriteErrorLine("Error : The cache'{0}' does not exist on server {1}:{2} .", CacheName, NCache.ServerName, NCache.Port);
                    return;
                }

                if (config.CacheType.ToLower().Equals("local-cache"))
                {
                    OutputProvider.WriteLine("Local Cache cannot be added as a bridge cache");
                    return;
                }
                //cacheServer = GetCacheServers(config.Cluster.GetAllConfiguredNodes());

                if (_bridgeServer != null)
                {
                    try
                    {
                        OutputProvider.WriteLine("Adding Cache To Bridge '{0}' on {1}:{2}.", BridgeId, _bridgeService.ServerName, _bridgeService.Port);
                        Alachisoft.NCache.Bridging.Configuration.BridgeConfiguration bridgeConfig = _bridgeServer.GetBridgeConfiguration(BridgeId);

                        if(bridgeConfig==null)
                        {
                            OutputProvider.WriteErrorLine("No Bridge with Bridge ID '{0} exists' on Server {1}:{2}.", BridgeId, _bridgeService.ServerName, _bridgeService.Port);
                            return;
                        }
                        TargetCacheCofiguration targtCacheConfig = new TargetCacheCofiguration();
                        targtCacheConfig.CacheID = CacheName;
                        targtCacheConfig.CacheAlias = Alias; // set the Alias, null if name is different.
                        Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(CacheName);
                        string servers = String.Empty;
                        foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            servers += node.IpAddress + ",";
                            
                        }

                        servers = servers.Remove(servers.Length - 1);
                        targtCacheConfig.Servers = servers;

                        targtCacheConfig.IsConnected = true;
                        if (State.Equals(BridgeCacheStateParam.Active))
                        {
                            if (!VerifyBridgeMasterCache(BridgeId, false, bridgeConfig))
                            {
                                targtCacheConfig.IsMaster = true;
                            }
                            targtCacheConfig.Status = BridgeCacheStateParam.Active.ToString();

                        }
                        else
                            targtCacheConfig.Status = BridgeCacheStateParam.Passive.ToString();

                        ToolsUtil.VerifyBridgeConfigurations(bridgeConfig, BridgeId);       
                        List<TargetCacheCofiguration> previouslyAddedCaches = bridgeConfig.TargetCacheConfigList;

                        if (previouslyAddedCaches.Count <= 1)
                        {
                            //checking validations regarding bridge
                            foreach (TargetCacheCofiguration pCache in previouslyAddedCaches)
                            {

                                if (pCache.CacheID.ToLower().Equals(targtCacheConfig.CacheID.ToLower()))
                                {
                                    OutputProvider.WriteErrorLine("Failed to Add Cache to bridge '{0}'. Error:No Same Cache Can be Added Twice ", BridgeId);
                                    return;
                                }

                                if (pCache.Status.Equals("passive"))
                                {
                                    OutputProvider.WriteErrorLine("Failed to Add Cache to bridge '{0}'. Error:No both bridge caches can be passive ", BridgeId);
                                    return;
                                }

                            }
                        }
                        else
                        {
                            OutputProvider.WriteErrorLine("Failed to Add Cache to bridge '{0}'. Error:No More than 2 caches can be add at a time ", BridgeId);
                            return;
                        }
                        //   

                        //Adding Bridge to config.ncconf 
                        BridgeConfig bridgeConf = new BridgeConfig();
                        bridgeConf.CacheAlias = Alias;
                        bridgeConf.Id = BridgeId;
                        if (config != null)
                        {
                            bridgeConf.Servers = bridgeConfig.BridgeNodes;
                            bridgeConf.Port = bridgeConfig.BridgePort;
                            bridgeConf.Status = State.ToString();
                        }

                        config.Bridge = bridgeConf;


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

                        
                        Alachisoft.NCache.Config.NewDom.CacheServerConfig nConfig= cacheServer.GetNewConfiguration(CacheName) ;

                        foreach (Address node in nConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                        {
                            NCache.ServerName = node.IpAddress.ToString();
                            ICacheServer server = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            server.ConfigureBridgeToCache(config, userId, paswd, true);
                            server.HotApplyBridgeReplicator(CacheName, false);



                        }

                        ConveyToRegisteredNodes();



                        char [] separater={','};
                        bridgeConfig.TargetCacheConfigList.Add(targtCacheConfig);
                        // write in all bridge nodes bridge.nconnf file
                        bridgeConfig.DeploymentVersion++;
                        foreach (string bridgeIp in bridgeConfig.BridgeNodes.Split(separater).ToList())
                        {
                            _bridgeService = new NCBridgeRPCService(bridgeIp);
                            _bridgeServer = _bridgeService.GetBridgeServer(TimeSpan.FromSeconds(30));


                            _bridgeServer.RegisterBridge(bridgeConfig, true, true);

                        }

                        OutputProvider.WriteLine("{0} successfully added to BridgeID {1}", CacheName, BridgeId);

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
#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName; //bin folder
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
                TOOLNAME = "Add-BridgeCache Cmdlet";
                AddBridgeCache();
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
