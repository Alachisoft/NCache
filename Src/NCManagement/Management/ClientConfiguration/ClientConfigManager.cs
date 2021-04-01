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
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Configuration;
using System.Collections;
using System.IO;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Config.Dom;
using System.Threading;
using System.Globalization;

namespace Alachisoft.NCache.Management.ClientConfiguration
{
    class ClientConfigManager
    {
        static ArrayList ipAddresses = new ArrayList(1);
        static string c_configDir = DIRNAME;
        static string c_configFileName = FILENAME;
        static string DIRNAME = "config";

        static string FILENAME = "client.ncconf";

        internal static string ENDSTRING = "\r\n";
        static string bindIp;
        private static string _localCacheGivenId = String.Empty;

        public static int _serverPort = CacheServer.SocketServerPort;
        
        static Hashtable _caches = Hashtable.Synchronized(new Hashtable());
        static ClientConfiguration.Dom.ClientConfiguration _configuration;
        static object _lock = new object();
        static private bool _keepAlive;
        static private int _keepAliveInterval;


        public static void AvailableNIC(Hashtable nic)
        {
            if (nic != null)
            {
                ipAddresses.AddRange(nic.Keys);
            }
        }

        public static void LoadConfiguration()
        {
            try
            {
                CombinePath();

                if (!File.Exists(c_configFileName))
                {
                    /// Save a dummy configuration.
                    SaveConfiguration();
                    return;
                }

                _caches.Clear();
                LoadXml();
            }
            catch (ManagementException)
            {
                c_configFileName = "";
                throw;
            }
            catch (Exception e)
            {
                c_configFileName = "";
                throw new ManagementException(e.Message, e);
            }
        }

        public static string BindIP
        {
            get { return bindIp; }
            set { bindIp = value; }
        }

        public static string LocalCacheId
        {
            get { return _localCacheGivenId; }
            set { _localCacheGivenId = value; }
        }

        public static Boolean KeepAlive
        {

            get { return _keepAlive; }
            set { _keepAlive = value; }

        }

        public static int KeepAliveInterval
        {
            get { return _keepAliveInterval; }
            set { _keepAliveInterval = value; }
        }


        private static void CombinePath()
        {
            c_configDir = AppUtil.InstallDir;

            if (c_configDir == null || c_configDir.Length == 0)
            {
                throw new ManagementException("Missing installation folder information");
            }

            c_configDir = Path.Combine(c_configDir, DIRNAME);
            if (!Directory.Exists(c_configDir))
                Directory.CreateDirectory(c_configDir);

            c_configFileName = Path.Combine(c_configDir, FILENAME);
        }

        private static void LoadXml()
        {
            if (String.IsNullOrEmpty(c_configFileName))
                CombinePath();

            ConfigurationBuilder configBuilder = new ConfigurationBuilder(c_configFileName);
            configBuilder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Management.ClientConfiguration.Dom.ClientConfiguration));
            configBuilder.ReadConfiguration();

            ClientConfiguration.Dom.ClientConfiguration clientConfiguration = null;
            Object[] configuration = configBuilder.Configuration;

            if (configuration != null && configuration.Length > 0)
            {
                for (int i = 0; i < configuration.Length; i++)
                {
                    clientConfiguration = configuration[i] as ClientConfiguration.Dom.ClientConfiguration;
                    break;
                }
            }

            _configuration = clientConfiguration;

            if (_configuration == null)
                _configuration = new Alachisoft.NCache.Management.ClientConfiguration.Dom.ClientConfiguration();

            if (_configuration.NodeConfiguration == null)
                _configuration.NodeConfiguration = new NodeConfiguration();

            KeepAlive = false;
            KeepAliveInterval = 30;
            _configuration.BindIp = BindIP;
        }

        public static void UpdateCacheConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration)
        {
            LoadXml();

            if (_configuration == null)
                _configuration = new Alachisoft.NCache.Management.ClientConfiguration.Dom.ClientConfiguration();

            if (_configuration.CacheConfigurationsMap == null)
                _configuration.CacheConfigurationsMap = new Dictionary<string, CacheConfiguration>();

            cacheId = cacheId.ToLower();
            _configuration.NodeConfiguration = configuration.NodeConfiguration.Clone() as NodeConfiguration;
            CacheConfiguration cacheConfiguration = null;
            if (configuration.CacheConfigurationsMap.TryGetValue(cacheId, out cacheConfiguration))
            {
                cacheConfiguration.BindIp = configuration.BindIp;
                _configuration.CacheConfigurationsMap[cacheId] = cacheConfiguration.Clone() as CacheConfiguration;
            }

            SaveConfiguration();
        }

        public static void RemoveCacheServer(string cacheId, string server)
        {
            LoadXml();

            if (_configuration != null && _configuration.CacheConfigurationsMap != null)
            {
                cacheId = cacheId.ToLower();
                if (_configuration.CacheConfigurationsMap.ContainsKey(cacheId))
                {
                    CacheConfiguration cacheConfig = _configuration.CacheConfigurationsMap[cacheId];
                    cacheConfig.RemoveServer(server);
                }
            }

            SaveConfiguration();
        }

        public static void RemoveCache(string cacheId)
        {

            LoadXml();

            if (_configuration != null && _configuration.CacheConfigurationsMap != null)
            {
                cacheId = cacheId.ToLower();
                if (_configuration.CacheConfigurationsMap.ContainsKey(cacheId))
                {
                    _configuration.CacheConfigurationsMap.Remove(cacheId);
                }
            }

            SaveConfiguration();
        }

        public static void AddCache(string cacheId, int cachePort, RtContextValue serverRuntimeContext)
        {
            LoadXml();

            if (_configuration != null)
            {
                if (_configuration.CacheConfigurationsMap == null)
                    _configuration.CacheConfigurationsMap = new Dictionary<string, CacheConfiguration>();

                if (!_configuration.CacheConfigurationsMap.ContainsKey(cacheId.ToLower()))
                {
                    CacheConfiguration cacheConfiguration = new CacheConfiguration();
                    cacheConfiguration.CacheId = cacheId;
                    cacheConfiguration.ServerRuntimeContext = serverRuntimeContext;
                    cacheConfiguration.AddLocalServer();
                    _configuration.CacheConfigurationsMap[cacheId.ToLower()] = cacheConfiguration;
                }
            }

            SaveConfiguration();
        }

        public static void AddCache(string cacheId, CacheServerConfig config)
        {
            LoadXml();

            if (_configuration != null)
            {
                if (_configuration.CacheConfigurationsMap == null)
                    _configuration.CacheConfigurationsMap = new Dictionary<string, CacheConfiguration>();

                if (!_configuration.CacheConfigurationsMap.ContainsKey(cacheId.ToLower()))
                {
                    CacheConfiguration cacheConfiguration = new CacheConfiguration();
                    cacheConfiguration.CacheId = cacheId;
                    cacheConfiguration.BindIp = BindIP; // Internal static property set for local node.
                    if (config.Cluster != null && config.Cluster.Nodes != null)
                    {
                        if (!String.IsNullOrEmpty(config.Cluster.Topology))
                            cacheConfiguration.LoadBalance = config.Cluster.Topology.Equals("replicated-server");

                        if (config.Cluster.Nodes.Count > 1)
                        {
                            // Extract server names from config
                            string[] serverList = new string[config.Cluster.NodeIdentities.Count];
                            for (int i = 0; i < serverList.Length; i++)
                            {
                                serverList[i] = config.Cluster.NodeIdentities[i].NodeName;
                            }

                            // Sort priority list i.e. local node at top
                            string[] copyServerList = new string[serverList.Length];
                            int nodeCount = 1;
                            int bindIPPosition = Array.IndexOf(serverList, cacheConfiguration.BindIp);
                            if (bindIPPosition == -1)
                            {
                                for (int serverListCount = 0; serverListCount < serverList.Length; serverListCount++)
                                {
                                    cacheConfiguration.AddServer(serverList[serverListCount], serverListCount);
                                }
                            }
                            else
                            {
                                copyServerList[0] = serverList[bindIPPosition];
                                cacheConfiguration.AddServer(copyServerList[0], 0);
                                for (int serverListCount = 0; serverListCount < serverList.Length; serverListCount++)
                                {
                                    if (serverListCount == bindIPPosition)
                                        continue;

                                    copyServerList[nodeCount] = serverList[serverListCount];
                                    cacheConfiguration.AddServer(copyServerList[nodeCount], nodeCount);

                                    nodeCount++;
                                }
                            }
                        }
                        else
                        {
                            if (config.Cluster.Nodes.Count>0)
                                cacheConfiguration.AddServer(config.Cluster.NodeIdentities[0].NodeName, 0);
                        }
                    }
                    else
                    {
                        cacheConfiguration.AddLocalServer();
                    }
                    _configuration.CacheConfigurationsMap[cacheId.ToLower()] = cacheConfiguration;
                }
            }

            SaveConfiguration();

        }

        public static void SaveConfiguration()
        {
            if (c_configFileName == null || c_configFileName == "")
                CombinePath();

            FileStream fs = null;
            StreamWriter sw = null;
            lock (_lock)
            {
                try
                {
                    fs = new FileStream(c_configFileName, FileMode.Create);
                    sw = new StreamWriter(fs);
                    sw.Write(ToXml());
                    sw.Flush();
                }
                catch (Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }
                finally
                {
                    if (sw != null) sw.Close();
                    if (fs != null) fs.Close();
                }
            }
        }

        private static string ToXml()
        {
            StringBuilder sb = new StringBuilder();
         
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");

            sb.Append(ENDSTRING + "<!-- Client configuration file is used by client to connect to out-proc caches. " +
                ENDSTRING + "Light weight client also uses this configuration file to connect to the remote caches. " +
                ENDSTRING + "This file is automatically generated each time a new cache/cluster is created or " +
                ENDSTRING + "cache/cluster configuration settings are applied. Additionally security information " +
                ENDSTRING + "can be provided for each cache in the following format. " +
                ENDSTRING + "<security>" +
                ENDSTRING + "\t<primary user-id=\"domain\\pri-user\" password=\"pri-pass\"/>" +
                ENDSTRING + "\t<secondary user-id=\"domain\\sec-user\" password=\"sec-pass\"/>" +
                ENDSTRING + "</security>" +
                ENDSTRING + "-->");


            sb.Append("\n");
            CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture =
         new System.Globalization.CultureInfo("en-US");
                object[] configuration = new object[1];
                configuration[0] = _configuration;
                ConfigurationBuilder cfgBuilder = new ConfigurationBuilder(configuration);
                cfgBuilder.RegisterRootConfigurationObject(typeof(ClientConfiguration.Dom.ClientConfiguration));
                sb.Append(cfgBuilder.GetXmlString());
            }
            finally
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = cultureInfo;
            }
            return sb.ToString();
        }

        public static void UpdateServerNodes(string cacheId, Alachisoft.NCache.Management.ClientConfiguration.CacheServerList serversPriorityList, RtContextValue serverRuntimeContext)
        {
            LoadXml();

            if (_configuration != null)
            {
                if (_configuration.CacheConfigurationsMap == null)
                    _configuration.CacheConfigurationsMap = new Dictionary<string, CacheConfiguration>();

                cacheId = cacheId.ToLower();
                CacheConfiguration cacheConfiguration = null;
                
                if (_configuration.CacheConfigurationsMap.TryGetValue(cacheId, out cacheConfiguration))
                {
                    cacheConfiguration.ServersPriorityList = serversPriorityList.ServersList;
                }
                else
                {
                    cacheConfiguration = new CacheConfiguration();
                    cacheConfiguration.CacheId = cacheId;
                    cacheConfiguration.BindIp = bindIp;
                    cacheConfiguration.ServerRuntimeContext = serverRuntimeContext;
                    cacheConfiguration.ServersPriorityList = serversPriorityList.ServersList;
                    _configuration.CacheConfigurationsMap.Add(cacheId, cacheConfiguration);
                }
            }

            SaveConfiguration();
        }

        public static void UpdateServerNodes(string cacheId, string[] servers, ref string xml, string clientCacheId, bool loadBalance)
        {
            LoadXml();

            cacheId = cacheId.ToLower();
            CacheConfiguration cacheConfiguration = null;

            if (_configuration != null && _configuration.CacheConfigurationsMap != null)
            {
                if (!_configuration.CacheConfigurationsMap.TryGetValue(cacheId, out cacheConfiguration))
                {
                    cacheConfiguration = new CacheConfiguration();
                    cacheConfiguration.CacheId = cacheId;
                    _configuration.CacheConfigurationsMap.Add(cacheId, cacheConfiguration);
                }
            }

            cacheConfiguration.ServersPriorityList.Clear();

            for (int i = 0; i < servers.Length; i++)
            {
                Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer server = new Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer();
                server.ServerName = servers[i];
                server.Priority = i;
                cacheConfiguration.ServersPriorityList[i] = server;
            }
            
            cacheConfiguration.LoadBalance = loadBalance;

            SaveConfiguration();
            xml = string.Empty;
        }

      

      

        internal static int GetConfigurationId()
        {
            int configurationId = 0;

            LoadXml();
            if (_configuration != null && _configuration.NodeConfiguration != null)
                configurationId = _configuration.NodeConfiguration.ConfigurationId;

            return configurationId;
        }

        internal static ClientNodeStatus GetClientNodeStatus(string cacheId, out string clientCacheId)
        {
            ClientNodeStatus status = ClientNodeStatus.Unavailable;
            clientCacheId = string.Empty;

            LoadXml();

            if (_configuration != null && _configuration.CacheConfigurationsMap != null)
            {
                CacheConfiguration config = null;
            }

            return status;
        }

        internal static ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId)
        {
            LoadXml();
            return _configuration;
        }
    }
}
