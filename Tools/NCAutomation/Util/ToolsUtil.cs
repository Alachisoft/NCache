using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Client;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.NewDom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Automation.Util
{
    public class ToolsUtil
    {

        public static bool VerifyClusterConfigurations(Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig
          , string cacheName, List<string> nodesToSkip = null)
        {
            if (serverConfig == null)
            {
                throw new Exception("Specified cache is not registered on the given server");
            }
            double configVersion = -1;
            double deploymentVersion = -1;
            string configId = "dummyconfig";
            NCacheRPCService NCache = new NCacheRPCService("");
            if (serverConfig.CacheSettings.CacheType == "clustered-cache")
            {
                foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                {
                    NCache.ServerName = node.IpAddress.ToString();
                    ICacheServer cacheServer = null;
                    try
                    {
                        if (nodesToSkip != null && (nodesToSkip.Contains(NCache.ServerName))) continue;
                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    }
                    catch (Exception ex)
                    {
                        if (nodesToSkip != null && (nodesToSkip.Contains(NCache.ServerName))) continue;
                        throw ex;
                    }
                    ConfigurationVersion config = cacheServer.GetConfigurationVersion(cacheName);
                    if (configId.Equals(configId))
                    {
                        configId = config.ConfigID;
                    }

                    else if (!configId.Equals(config.ConfigID, StringComparison.InvariantCultureIgnoreCase))
                    {
                        throw new VersionException("same name cache is already registered on node : " + node.IpAddress, 0);
                    }

                    if (configVersion == -1)
                    {
                        configVersion = config.ConfigVersion;
                    }
                    else if (configVersion != config.ConfigVersion)
                    {
                        throw new VersionException("There is a configuration mismatch for caches : " + cacheName, 0);
                    }

                    if (deploymentVersion == -1)
                    {
                        deploymentVersion = config.DeploymentVersion;
                    }
                    else if (deploymentVersion != config.DeploymentVersion)
                    {
                        throw new Exception("Deployement version is not same on all nodes");
                    }

                    cacheServer.Dispose();

                }
            }
            return true;
        }

        public static void ConveyCommandToAllRunningCacheHost(ConfiguredCacheInfo[] cacheHostprocesses, string Action, string server, IOutputConsole OutputProvider)
        {
            foreach (ConfiguredCacheInfo cacheHost in cacheHostprocesses)
            {

                NCacheRPCService nCache = new NCacheRPCService("");
                nCache.Port = cacheHost.ManagementPort;
                nCache.ServerName = server;
                if (nCache.Port > 0)
                {
                    ICacheServer hostService = nCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    if (Action.ToLower().Equals("start"))
                    {

                        OutputProvider.WriteLine("Starting monitoring on server {0}:{1}.", nCache.ServerName, nCache.Port);
                        hostService.StartMonitoringActivity();

                    }
                    else if (Action.ToLower().Equals("stop"))
                    {
                        OutputProvider.WriteLine("Stop monitoring on server {0}:{1}.", nCache.ServerName, nCache.Port);
                        hostService.StopMonitoringActivity();

                    }
                    hostService.PublishActivity();
                }

            }

        }

    public static void PrintLogo(IOutputConsole outputProvider , bool printLogo, string TOOLNAME)


        {
            if (printLogo)
            {
                string logo = @"Alachisoft (R) NCache Utility "+ TOOLNAME+". Version " +
                    @"
Copyright (C) Alachisoft 2026. All rights reserved.";

                outputProvider.WriteLine(logo);
                outputProvider.WriteLine(Environment.NewLine);
            }
        }

        public static bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip.Trim(), out adress);
        }

        public static void PrintMessage(IOutputConsole outputProvider,string msg)
        {
            outputProvider.WriteErrorLine(msg);
        }

        public static CacheConnectionOptions AddServersInCacheConnectionOptions(string server, CacheConnectionOptions options)
        {
            if (!String.IsNullOrEmpty(server))
            {
                ServerInfo[] cacheServers;
                string[] servers = server.Split(new char[] { ',' });

                if (servers.Length > 0)
                {
                    cacheServers = new ServerInfo[servers.Length];
                    for (int i = 0; i < cacheServers.Length; i++)
                    {
                        ServerInfo serverinfo = new ServerInfo(servers[i], 9800);
                        cacheServers[i] = serverinfo;
                    }
                    options.ServerList = cacheServers;
                }

            }
            return options;
        }

        public static string GetTopologyName(CacheTopologyParam? Topology)
        {
            string topologyName = "";
            switch (Topology)
            {
                case CacheTopologyParam.Local:
                    topologyName = "local";
                    break;

                case CacheTopologyParam.Replicated:
                    topologyName = "replicated";
                    break;

                default:
                    topologyName =null;
                    break;
            }
            return topologyName;
        }

        public static Common.Enum.CacheTopology GetTopologyType(string topologyName)
        {

            Common.Enum.CacheTopology topology = new Common.Enum.CacheTopology();
            switch (topologyName.ToLower())
            {
                case "local":
                    topology = Common.Enum.CacheTopology.Local;
                    return topology;
                case "replicated":
                    topology = Common.Enum.CacheTopology.Replicated;
                    return topology;

                case "partitioned":
                    topology = Common.Enum.CacheTopology.Partitioned;
                    return topology;
                case "mirrored":
                    topology = Common.Enum.CacheTopology.Mirror;
                    return topology;

                default:
                    throw new Exception("Invalid Topology name");

            }
        }

        public static bool IsNcacheInstalled(IOutputConsole OutputProvider)
        {
            bool isInstalled = true;
            if (AppUtil.InstallDir != null)
            {
                if (!AppUtil.InstallDir.ToLower().Contains("ncache") || !Directory.Exists(Path.Combine(AppUtil.InstallDir, "bin", "service")))
                {
                    OutputProvider.WriteErrorLine("This cmdlet can only be executed on machines where NCache is installed.");
                    isInstalled = false;
                }
            }
            return isInstalled;
        }

        internal static string GetServiceBindedIP()
        {
            string ncacheDirectory = Path.Combine(AppUtil.InstallDir, "bin", "service");
            IPAddress address = default(IPAddress);
            if (Directory.Exists(ncacheDirectory))
            {
                string serviceConfig = Path.Combine(ncacheDirectory, "Alachisoft.NCache.Service.exe.config");
                try
                {
                    if (File.Exists(serviceConfig))
                        serviceConfig = Path.Combine(ncacheDirectory, "Alachisoft.NCache.Service.exe");
                    else

                        serviceConfig = Path.Combine(ncacheDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
                            ? "Alachisoft.NCache.Daemon.dll" : "Alachisoft.NCache.Service.dll");


                    Configuration config = ConfigurationManager.OpenExeConfiguration(serviceConfig);

                    if (config != null && config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                        address = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                }
                catch { }
            }

            return address == null ? string.Empty : address.ToString();
        }

        internal static bool IsDistributedModule(CacheServerConfig config)
        {
            if (config == null) return false;
            return ((config.CacheSettings.CacheTopology.Topology.Equals("partitioned") || config.CacheSettings.CacheTopology.Topology.Equals("partitioned-replica")) || config.CacheSettings.CacheTopology.Topology.Equals("local"));
        }
        public static string GetStore(StoreType? storeType)
        {
            if (storeType.HasValue)
                return StoreTypeUtil.GetStore(storeType.Value);

            return StoreTypeUtil.DISTRIBUTED_CACHE;
        }
        /// <summary>
        /// Gets stopre display name from config name
        /// </summary>
        /// <param name="store"></param>
        /// <param name="schemeName"></param>
        /// <returns></returns>
        public static string GetStoreDisplayName(string store, string schemeName = "")
        {
            if (store != null)
            {
                if (schemeName.Equals("local", StringComparison.InvariantCultureIgnoreCase))
                    return StoreTypeUtil.GetStoreDisplayName(store, true);
                return StoreTypeUtil.GetStoreDisplayName(store);
            }
            return string.Empty;
        }
        public static bool AreAllNodesAvailable(ArrayList servers)
        {
            if (servers == null) return true;
            foreach (Alachisoft.NCache.Config.NewDom.ServerNode server in servers)
            {
                try
                {
                    var cacheService = new NCacheRPCService(server.IP);
                    var nCacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool PromptUser(string message, bool isPowershell, ParameterBase pSCmdlet)
        {
            pSCmdlet.OutputProvider.WriteLine(message);
            string response = string.Empty;
            if (isPowershell)
            {
                ICollection<PSObject> resp = pSCmdlet.InvokeCommand.InvokeScript("Read-Host");

                foreach (PSObject r in resp)
                {
                    response = r.ToString();
                }
            }
            else
            {
                response = Console.ReadLine();
            }

            // returns true if user says yes(Y/y)
            return (response.ToLower().Equals("y") || response.ToLower().Equals("yes"));
        }
        /// <summary>
        /// This method compares the topology filter that the user has provided with the topology of the cache.
        /// </summary>
        /// <returns></returns>
        public static bool CompareTopology(Common.Enum.CacheTopology cacheTopology, CacheTopologyParam? topologyToFilter)
        {
            if (cacheTopology == Common.Enum.CacheTopology.Local)
            {
                return topologyToFilter == CacheTopologyParam.Local;
            }
            else if (cacheTopology == Common.Enum.CacheTopology.Replicated)
            {
                return topologyToFilter == CacheTopologyParam.Replicated;
            }

            return false;
        }
    }
}
