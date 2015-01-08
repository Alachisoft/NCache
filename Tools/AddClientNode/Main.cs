// Copyright (c) 2015 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Config.Dom;
using System.Collections;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Tools.Common;
using System.Net;
using System.Diagnostics;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Management.ClientConfiguration;
using Alachisoft.NCache.Config;

namespace Alachisoft.NCache.Tools.AddClientNode
{
    class Program
    {
        static void Main(string[] args)
        {
            AddClientNodeTool.Run(args);
        }
    }

    public class AddClientNodeParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {


        private string _cacheId = string.Empty;
        private int _port = -1;
        private string _server = string.Empty;
        private string _clientNode = string.Empty;
        private bool _updateServerConfig=true;
        private bool _acquireServerMapping=false;

        public AddClientNodeParam()
        {
        }

        [ArgumentAttribute("", "")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ArgumentAttribute(@"/s", @"/server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/e", @"/client-node")]
        public string ClientNode
        {
            get { return _clientNode; }
            set { _clientNode = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }

        [ArgumentAttribute(@"/u", @"/update-server-config",true)]
        public bool UpdateServerConfig
        {
            get { return _updateServerConfig; }
            set { _updateServerConfig = value; }
        }

        [ArgumentAttribute(@"/a", @"/acquire-server-mapping",false)]
        public bool AcquireServerMapping
        {
            get { return _acquireServerMapping; }
            set { _acquireServerMapping = value; }
        }
    }

    sealed class AddClientNodeTool
    {
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static private CacheServerConfig clientCacheConfig;
        
        static private bool clientCacheExists = false;
        static private List<string> newClientNodes = new List<string>();
        static private List<string> currentServerNodes = new List<string>();
        static private List<string> currentClientNodes = new List<string>();
        static private Alachisoft.NCache.Config.NewDom.CacheServerConfig config = null;
        static private ClientConfiguration clientConfig;
        static private Alachisoft.NCache.Management.ICacheServer cacheServer;
        static private AddClientNodeParam cParam = new AddClientNodeParam();
        static private ServerMapping _clientIPMapping = null;
        static private Dictionary<string, Mapping> _managementIPMapping = new Dictionary<string, Mapping>();
        static private string[] _serverList = null;
        static private int index = 0;
        static private string _server = null;
        


        static public bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip, out adress);
        }

        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
                return false;
            }
                           
            if (string.IsNullOrEmpty(cParam.Server))
            {
                Console.Error.WriteLine("Error: Server not specified.");
                return false;
            }
            if (!cParam.Server.Contains(",") && !cParam.Server.Contains(":"))
            {
                if (!IsValidIP(cParam.Server))
                {
                    Console.Error.WriteLine("Error: Invalid Server IP.");
                    return false;
                }
            }

            if (string.IsNullOrEmpty(cParam.ClientNode))
            {
                Console.Error.WriteLine("Error: ClientNode not specified.");
                return false;
            }

            if (!IsValidIP(cParam.ClientNode))
            {
                Console.Error.WriteLine("Error: ClientNode(IP) is invalid.");
                return false;
            }
            AssemblyUsage.PrintLogo(cParam.IsLogo);

            return true;
        }

        private static void LogEvent(string msg)
        {
            EventLogEntryType type = EventLogEntryType.Error;
            using (EventLog ncLog = new EventLog("Application"))
            {
                ncLog.Source = "NCache:AddClient Node Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>
        static public void Run(string[] args)
        {
           
            try
            {
                object param = new AddClientNodeParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (AddClientNodeParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }
               
                if (!ValidateParameters()) return;
                bool multipleServers = false;
                if (cParam.Server != null || cParam.Server != string.Empty)
                {
                    _serverList = cParam.Server.Split(',');
                    if (_serverList.Length > 1 || (_serverList[0].Contains(":")))
                    {
                        multipleServers = true;
                    }
                    
                }
                tryNextServer:
                if (multipleServers)
                {
                    string[] serverAddress = _serverList[index].Split(':');
                    if (serverAddress.Length == 2)
                    {
                        if (!IsValidIP(serverAddress[0]))
                        {
                            Console.Error.WriteLine("Error: Invalid Server IP.");
                            return;
                        }
                        NCache.ServerName = serverAddress[0];
                        try
                        {
                            NCache.Port = Convert.ToInt32(serverAddress[1]);
                        }
                        catch (Exception)
                        {
                            throw new Exception("Invalid Port :" + serverAddress[1] + " specified for server : " + serverAddress[0] + ".");
                        }

                    }
                    else
                        if (serverAddress.Length == 1)
                        {
                            if (!IsValidIP(serverAddress[0]))
                            {
                                Console.Error.WriteLine("Error: Invalid Server IP.");
                                return;
                            }
                            NCache.ServerName = serverAddress[0];
                            NCache.Port = cParam.Port;
                            if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                        }
                        else
                        {
                            throw new Exception("Invalid server Address specified, kindly specify as [IPAdress] or [IPAdress]:[Port].");
                        }
                }
                else
                {
                    if (cParam.Port != -1)
                    {
                        NCache.Port = cParam.Port;
                    }
                    if (cParam.Server != null || cParam.Server != string.Empty)
                    {
                        NCache.ServerName = cParam.Server;
                    }
                    if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                }

                
                try
                {
                    _server = NCache.ServerName;
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception ex)
                {
                    if (multipleServers)
                    {
                        if (index  < _serverList.Length - 1)
                        {
                            Console.Error.WriteLine("Failed to connect to server : " + NCache.ServerName + ":"+NCache.Port+" \nTrying next server.");
                            index++;
                            goto tryNextServer;
                        }
                    }
                    throw ex;
                }
                config = cacheServer.GetNewConfiguration(cParam.CacheId);
                //for getting server mappings
                if (cParam.AcquireServerMapping)
                {
                    Alachisoft.NCache.Management.MappingConfiguration.Dom.MappingConfiguration mapping=cacheServer.GetServerMappingForClient();
                    if (mapping != null)
                    {
                        _clientIPMapping = mapping.ClientIPMapping;
                        foreach (Mapping mappingServer in mapping.ManagementIPMapping.MappingServers)
                        {
                            if (mappingServer != null)
                            {
                                _managementIPMapping.Add(mappingServer.PrivateIP, mappingServer);
                            }
                        }
                    }
                }
                if (config == null)
                {
                    Console.Error.WriteLine("Error : The cache'{0}' does not exist on server {1}:{2} .", cParam.CacheId, NCache.ServerName, NCache.Port);
                    return;
                }
                if (config.CacheSettings.CacheType == "clustered-cache")
                {
                    Console.WriteLine("Adding client node '{0}' to cache '{1}' on server {2}:{3}.",
                          cParam.ClientNode, cParam.CacheId, NCache.ServerName, NCache.Port);
                    foreach (Address node in config.CacheDeployment.Servers.GetAllConfiguredNodes())
                    {
                        currentServerNodes.Add(node.IpAddress.ToString());
                    }
                }
                else
                {
                    Console.Error.WriteLine("Error: Client nodes cannot be added to local caches");
                    return;
                }
                if (config.CacheDeployment.ClientNodes != null)
                {

                    foreach (ClientNode clientNode in config.CacheDeployment.ClientNodes.NodesList)
                    {
                       
                        if (cParam.ClientNode.Equals(clientNode.Name))
                        {
                            Console.Error.WriteLine("Error: " + clientNode.Name + " already part of \"" + cParam.CacheId + "\"");
                            return;
                        }
                        currentClientNodes.Add(clientNode.Name);
                    }
                }

                UpdateConfigs();
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
            }
        }

		private static bool ValidateNCacheService()
        {
            ICacheServer cacheServer;
            try
            {
                if (cParam.UpdateServerConfig)
                {
                    //Validate Service on Server(s)
                    foreach (string node in currentServerNodes)
                    {
                        NCache.ServerName = node;
                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    }
                }
                //Validate Service on Client
                NCacheRPCService nCache = new NCacheRPCService("");
                nCache.ServerName = cParam.ClientNode; //clientNode;
                cacheServer = nCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                return false;
            }
        }
		
		
        private static void UpdateConfigs()
        {
            try
            {
				if (ValidateNCacheService())
                {
                if (cParam.UpdateServerConfig)
                {
                    if (config != null)
                    {
                        if (config.CacheDeployment.ClientNodes == null)
                            config.CacheDeployment.ClientNodes = new ClientNodes();

                        if (config.CacheDeployment.ClientNodes.NodesList == null)
                            config.CacheDeployment.ClientNodes.NodesList = new List<ClientNode>();
                    }

                        ClientNode clientNod = new ClientNode();
                        clientNod.Name = cParam.ClientNode;
                       
                    if(config != null)
                        config.CacheDeployment.ClientNodes.NodesList.Add(clientNod);
  
                
                    foreach (string node in currentServerNodes)
                    {
                        NCache.ServerName = node;
                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                   
                        cacheServer.ApplyCacheConfiguration(cParam.CacheId, config, false);
                    }
                    string oldClientNode = null;
                    ClientConfiguration clientConfig = null;
                    if (currentClientNodes.Count > 0)
                    {
                        oldClientNode = currentClientNodes[0];
                        NCache.ServerName = oldClientNode;
                        cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                        clientConfig = cacheServer.GetClientConfiguration(cParam.CacheId);
                    }

                }
                
                    NCacheRPCService nCache = new NCacheRPCService("");
                    nCache.ServerName = cParam.ClientNode; //clientNode;
                    cacheServer = nCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    string xml = string.Empty;
                    cacheServer.UpdateClientServersList(cParam.CacheId, GetSeversPriorityList(),"0");
                    ClientConfiguration clientConfiguration = cacheServer.GetClientConfiguration(cParam.CacheId);
                    CacheConfiguration cacheConfig = new CacheConfiguration();
                    cacheConfig = clientConfiguration.CacheConfigurationsMap[cParam.CacheId.ToLower()];
                    //if flag of get mapping is true
                    if (cParam.AcquireServerMapping && _clientIPMapping != null)
                    {
                        if (cacheConfig.ServerMapping == null)
                        {
                            cacheConfig.ServerMapping = new ServerMapping();
                        }
                        cacheConfig.ServerMapping = new ServerMapping(Management.Management.Util.ManagementWorkFlow.GetUpdatedMappingList(cacheConfig.ServerMapping.MappingServers, _clientIPMapping.MappingServers));
                    }
                   
                    clientConfiguration.CacheConfigurationsMap.Remove(cParam.CacheId);
                    clientConfiguration.CacheConfigurationsMap.Add(cParam.CacheId, cacheConfig);
                    if (config != null)
                    {
                        if (config.CacheDeployment.Servers.NodeIdentities != null && config.CacheDeployment.Servers.NodeIdentities.Count != 0)
                        {
                            cacheConfig.ServersPriorityList.Clear();

                            foreach (NodeIdentity identity in config.CacheDeployment.Servers.NodeIdentities)
                            {
                                Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer server = new Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer();
                                server.ServerName = identity.NodeName;
                                server.Priority = identity.NodePriority;
                                cacheConfig.ServersPriorityList[identity.NodePriority - 1] = server;
                            }
                        }
                    }
                    cacheServer.UpdateClientConfiguration(cParam.CacheId, clientConfiguration);
     
                    Console.WriteLine("{0}' successfully added as client node to cache '{1}' on server {2}:{3}.",
                        cParam.ClientNode, cParam.CacheId, NCache.ServerName, NCache.Port);
            }
			}
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
            }
        }

        private static CacheServerList GetSeversPriorityList()
        {
            
            Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer> serversPriorityList = new Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer>();
            Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer server = new Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer();
            if (config != null)
            {
                List<Address> hosts = config.CacheDeployment.Servers.GetAllConfiguredNodes();
                Alachisoft.NCache.Management.ICacheServer cs;
                NCacheRPCService cache = new NCacheRPCService("");
                int priority = 0;
                int port = -1;
                foreach (Address addr in hosts)
                {
                    try
                    {
                        string address = addr.IpAddress.ToString();
                        if (_managementIPMapping != null)
                        {
                            if (_managementIPMapping.ContainsKey(address))
                            {
                                Mapping endPoint = _managementIPMapping[address];
                                address = endPoint.PublicIP;
                                port = endPoint.PublicPort;
                            }
                        }
                        server = new Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer();
                        string tempClientServerName = "";
                        cache.ServerName = address;
                        if (port != -1)
                            cache.Port = port;
                        cs = cache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                        Management.Management.BindedIpMap bindedIps = cs.BindedIp();

                        if (bindedIps.Map.Contains(Alachisoft.NCache.Management.CacheServer.Channel.SocketServer))
                            tempClientServerName = bindedIps.Map[Alachisoft.NCache.Management.CacheServer.Channel.SocketServer].ToString();

                        if (!string.IsNullOrEmpty(tempClientServerName))
                            server.ServerName = tempClientServerName;
                        else
                            server.ServerName = addr.IpAddress.ToString();

                        server.Priority = priority;

                        serversPriorityList.Add(priority, server);
                        priority++;
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error while adding server :" + cache.ServerName + ":" + cache.Port + "\nException : " + ex.Message);
                    }
                }
            }

            if (serversPriorityList.Count < 1)
            {
                server.ServerName = _server;
                server.Priority = 0;
                serversPriorityList.Add(0, server);
            }

            return new CacheServerList(serversPriorityList);
        }

        private static void RegisterClientCache(string clientCacheId, string node)
        {
            NCache.ServerName = node;
            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
            cacheServer.RegisterCache(clientCacheId, clientCacheConfig, string.Empty, true, false);
        }

        

    }
}
