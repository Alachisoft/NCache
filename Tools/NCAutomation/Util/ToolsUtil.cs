//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License
using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Client;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.Automation.Util
{
    public class ToolsUtil
    {

        public static bool VerifyClusterConfigurations(Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig
           , string cacheName)
        {
            if(serverConfig==null)
            {
                throw new Exception("Specified cache is not registered on the given server");
            }
            double configVersion = -1;
            double deploymentVersion =-1;
            string configId = "dummyconfig"; 
            NCacheRPCService NCache =new NCacheRPCService("");
            if (serverConfig.CacheSettings.CacheType == "clustered-cache")
            {
                foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                {
                    NCache.ServerName = node.IpAddress.ToString();

                    ICacheServer cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
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
        public static void PrintLogo(IOutputConsole outputProvider, bool printLogo, string TOOLNAME)


        {
            if (printLogo)
            {
                string logo = @"Alachisoft (R) NCache Utility " + TOOLNAME + ". Version NCache Open Source 5.0 SP5" +
                    @"
Copyright (C) Alachisoft 2021. All rights reserved.";

                outputProvider.WriteLine(logo);
                outputProvider.WriteLine(Environment.NewLine);
            }
        }


        public static bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip, out adress);
        }


        public static void PrintMessage(IOutputConsole outputProvider,string msg)
        {
            outputProvider.WriteErrorLine(msg);
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

                case CacheTopologyParam.Partitioned:
                    topologyName = "partitioned";
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
                case "mirrored":
                    topology = Common.Enum.CacheTopology.Mirror;
                    return topology;                
                    
                default:
                    throw new Exception("Invalid Topology name");

            }
        }


    }
}
