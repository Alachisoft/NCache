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
using System.Data;
using System.Collections;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Collections.Generic;

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;


using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Enum;
using System.Net;
using Alachisoft.NCache.Config.Dom;
using System.Diagnostics;
using Alachisoft.NCache.Common.Net;
using System.Threading;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Tools.Common;
using System.Text;
using System.IO;
using System.IO.Compression;
using Alachisoft.NCache.Management.ServiceControl;


namespace Alachisoft.NCache.Tools.CreateCache
{

    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                ConfigureCacheTool.Run(args);

            }
            catch (Exception e)
            {
            }
        }
    }
    /// <summary>
    /// Summary description for ConfigureCacheTool.
    /// </summary>
    /// 
    public class ConfigureCacheParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _cacheId = string.Empty;
        private string _path = string.Empty;
        private string _file = string.Empty;
        private string _server = string.Empty;
        private int _port = -1;
        private long _cacheSize = 1024;
        private string _evictionPolicy = string.Empty;
        private decimal _ratio = -1;
        private int _cleanupInterval = -1;
        private string _topology = string.Empty;
        private string _repStrategy = "async";
        private int _clusterPort = -1;
        private string _defPriority = string.Empty;
        private const string PARTITIONED_TOPOLOGY_NAME = "partitioned";
        private const string REPLICATED_TOPOLOGY_NAME = "replicated";
        private const string LOCAL_TOPOLOGY_NAME = "local";
        private bool _isInProc = false;

        

        private static bool IsValidTopologyName(string topology)
        {
            ArrayList topologyList = new ArrayList();

            topologyList.Add(PARTITIONED_TOPOLOGY_NAME);
            topologyList.Add(REPLICATED_TOPOLOGY_NAME);

            topologyList.Add(LOCAL_TOPOLOGY_NAME);

            if (topologyList.Contains(topology.ToLower()))
            {
                return true;
            }
            else
                return false;
        }

        public ConfigureCacheParam()
        {
        }


        [ArgumentAttribute("", "")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ArgumentAttribute(@"/I", @"/inproc", false)]
        public bool IsInProc
        {
            get { return _isInProc; }
            set { _isInProc = value; }
        }
        [ArgumentAttribute(@"/T", @"/path")]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

      
        public string FileName
        {
            get { return _file; }
            set { _file = value; }
        }

        [ArgumentAttribute(@"/s", @"/server")]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port < 1)
                    throw new ArgumentException("Invalid port(/p) value: cannot be less than 0");
                else
                    _port = value;
            }

        }

        [ArgumentAttribute(@"/S", @"/cache-size")]
        public long CacheSize
        {
            get { return _cacheSize; }
            set { _cacheSize = value; }
        }

        [ArgumentAttribute(@"/y", @"/evict-policy")]
        public string EvictionPolicy
        {
            get { return _evictionPolicy; }
            set { _evictionPolicy = value; }
        }

        [ArgumentAttribute(@"/o", @"/ratio")]
        public decimal Ratio
        {
            get { return _ratio; }
            set { _ratio = value; }
        }

        [ArgumentAttribute(@"/i", @"/interval")]
        public int CleanupInterval
        {
            get { return _cleanupInterval; }
            set { _cleanupInterval = value; }
        }

        [ArgumentAttribute(@"/t", @"/topology")]
        public string Topology
        {
            get { return _topology; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (IsValidTopologyName(value))
                    {
                        _topology = value;
                    }
                    else
                        throw new ArgumentException("Invalid topology name(/t)");
                }
                else
                    throw new ArgumentException("Topology name(/t) not specified");

            }
        }

        [ArgumentAttribute(@"/C", @"/cluster-port")]
        public int ClusterPort
        {
            get { return _clusterPort; }
            set { _clusterPort = value; }
        }

        [ArgumentAttribute(@"/d", @"/def-priority")]
        public string DefaultPriority
        {
            get { return _defPriority; }
            set { _defPriority = value; }
        }
    }

    sealed class ConfigureCacheTool
    {

        static private ConfigureCacheParam ccParam = new ConfigureCacheParam();
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static private Alachisoft.NCache.Config.NewDom.CacheServerConfig _SimpleCacheConfig = new Alachisoft.NCache.Config.NewDom.CacheServerConfig();
        /// <summary>
        /// Sets the application level parameters to those specified at the command line.
        /// </summary>
        /// <param name="args">array of command line parameters</param>

        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(ccParam.CacheId))
            {
                Console.Error.WriteLine(AppendBlankLine("Error: Cache name not specified."));
                return false;
            }

            

            if (string.IsNullOrEmpty(ccParam.Server))
            {
                Console.Error.WriteLine(AppendBlankLine("Error: Server IP not specified."));
                return false;
            }
            if (string.IsNullOrEmpty(ccParam.Topology))
            {
                ccParam.Topology = "local";
                return true;
            }

            if (!string.IsNullOrEmpty(ccParam.Topology) && ccParam.IsInProc)
            {
                if (!ccParam.Topology.Equals("local"))
                {
                    Console.Error.WriteLine(AppendBlankLine("Error: Cluster Cache cannot be InProc."));
                    return false;
                }
            }

            if (string.IsNullOrEmpty(ccParam.Path))
            {
                if (ccParam.Topology != null || ccParam.Topology != string.Empty)
                {
                    if (ccParam.CacheSize == -1)
                    {
                        Console.Error.WriteLine(AppendBlankLine("Error: Cache size not specified."));
                        return false;
                    }
                    if (ccParam.ClusterPort == -1 && !ccParam.Topology.Equals("local"))
                    {
                        Console.Error.WriteLine(AppendBlankLine("Error: Cluster port not specified."));
                        return false;
                    }

                }
                else
                {
                    Console.Error.WriteLine(AppendBlankLine("Error: Config path not specified. (For simple case specify topology)"));
                    return false;
                }
            }

            if (ccParam.Server == null || ccParam.Server == string.Empty)
            {
                Console.Error.WriteLine(AppendBlankLine("Error: Server IP not specified."));
                return false;
            }

            AssemblyUsage.PrintLogo(ccParam.IsLogo);

            return true;
        }

        ////<summary>
        ////Log an event in event viewer.
        ////</summary>
        private static void LogEvent(string msg)
        {
            EventLogEntryType type = EventLogEntryType.Error;
            using (EventLog ncLog = new EventLog("Application"))
            {
                ncLog.Source = "NCache: ConfigureCache Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>
        static public void Run(string[] args)
        {
            string failedNodes = string.Empty;
            NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            Alachisoft.NCache.Config.NewDom.CacheServerConfig[] caches = null;
            ICacheServer cacheServer = null;
            Alachisoft.NCache.Config.NewDom.CacheServerConfig _cacheConfig = null;

            try
            {
                object param = new ConfigureCacheParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                ccParam = (ConfigureCacheParam)param;

                if (ccParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(ccParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                if (ccParam.Port != -1)
                {
                    NCache.Port = ccParam.Port;
                }

                if (ccParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                if (ccParam.Path != null && ccParam.Path != string.Empty)
                {
                    if (Path.HasExtension(ccParam.Path))
                    {
                        string extension = Path.GetExtension(ccParam.Path);

                        if (!extension.Equals(".ncconf") && !extension.Equals(".xml"))
                        {
                            throw new Exception("Incorrect file format. Only .ncconf and .xml are supported.");
                        }
                    }
                    else
                        throw new Exception("Incorrect configuration file path specified.");

                    ConfigurationBuilder builder = new ConfigurationBuilder(ccParam.Path);
                    builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                    builder.ReadConfiguration();

                    if (builder.Configuration != null)
                    {
                        caches = new Alachisoft.NCache.Config.NewDom.CacheServerConfig[builder.Configuration.Length];
                        builder.Configuration.CopyTo(caches, 0);
                    }
                    else
                        throw new Exception("Configuration cannot be loaded.");
                    ConfigurationValidator validator = new ConfigurationValidator();
                    bool _isConfigValidated = validator.ValidateConfiguration(caches);

                    _cacheConfig = caches[0];

                    if (_cacheConfig.CacheSettings.Name == null)
                        _cacheConfig.CacheSettings.Name = ccParam.CacheId;

                    if (_cacheConfig.CacheSettings.Storage == null || _cacheConfig.CacheSettings.Storage.Size == -1)
                    {
                        throw new Exception("Cache size is not specified.");
                    }

                    if (_cacheConfig.CacheSettings.EvictionPolicy == null)
                    {
                        _cacheConfig.CacheSettings.EvictionPolicy = new EvictionPolicy();
                        _cacheConfig.CacheSettings.EvictionPolicy.Policy = "priority";
                        _cacheConfig.CacheSettings.EvictionPolicy.DefaultPriority = "normal";
                        _cacheConfig.CacheSettings.EvictionPolicy.EvictionRatio = 5;
                        _cacheConfig.CacheSettings.EvictionPolicy.Enabled = true;
                    }

                    if (_cacheConfig.CacheSettings.Cleanup == null)
                    {
                        _cacheConfig.CacheSettings.Cleanup = new Cleanup();
                        _cacheConfig.CacheSettings.Cleanup.Interval = 15;
                    }

                    if (_cacheConfig.CacheSettings.Log == null)
                    {
                        _cacheConfig.CacheSettings.Log = new Log();
                    }

                    if (_cacheConfig.CacheSettings.PerfCounters == null)
                    {
                        _cacheConfig.CacheSettings.PerfCounters = new PerfCounters();
                        _cacheConfig.CacheSettings.PerfCounters.Enabled = true;
                    }

                    if (_cacheConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        if (_cacheConfig.CacheSettings.CacheTopology.ClusterSettings == null)
                        {
                            throw new Exception("Cluster settings not specified for the cluster cache.");
                        }

                        if (_cacheConfig.CacheSettings.CacheTopology.ClusterSettings.Channel == null)
                        {
                            throw new Exception("Cluster channel related settings not specified for cluster cache.");
                        }

                        if (_cacheConfig.CacheSettings.CacheTopology.ClusterSettings.Channel.TcpPort == -1)
                        {
                            throw new Exception("Cluster port not specified for cluster cache.");
                        }
                    }
                }
                else
                {
                    _SimpleCacheConfig.CacheSettings = new Alachisoft.NCache.Config.NewDom.CacheServerConfigSetting();
                    _SimpleCacheConfig.CacheSettings.Name = ccParam.CacheId;
                    _SimpleCacheConfig.CacheSettings.Storage = new Alachisoft.NCache.Config.Dom.Storage();
                    _SimpleCacheConfig.CacheSettings.EvictionPolicy = new EvictionPolicy();
                    _SimpleCacheConfig.CacheSettings.Cleanup = new Cleanup();
                    _SimpleCacheConfig.CacheSettings.Log = new Log();
                    _SimpleCacheConfig.CacheSettings.PerfCounters = new PerfCounters();
                    _SimpleCacheConfig.CacheSettings.PerfCounters.Enabled = true;
                    _SimpleCacheConfig.CacheSettings.Storage.Type = "heap";
                    _SimpleCacheConfig.CacheSettings.Storage.Size = ccParam.CacheSize;
                    _SimpleCacheConfig.CacheSettings.EvictionPolicy.Policy = "priority";
                    _SimpleCacheConfig.CacheSettings.EvictionPolicy.DefaultPriority = "normal";
                    _SimpleCacheConfig.CacheSettings.EvictionPolicy.EvictionRatio = 5;
                    _SimpleCacheConfig.CacheSettings.EvictionPolicy.Enabled = false;
                    _SimpleCacheConfig.CacheSettings.Cleanup.Interval = 15;
                    _SimpleCacheConfig.CacheSettings.CacheTopology = new Alachisoft.NCache.Config.NewDom.CacheTopology();

                    if (string.IsNullOrEmpty(ccParam.Topology))
                    {
                        _SimpleCacheConfig.CacheSettings.CacheTopology.Topology = "Local";
                    }
                    else
                    {
                        _SimpleCacheConfig.CacheSettings.CacheTopology.Topology = ccParam.Topology;
                    }

                    if (ccParam.IsInProc && _SimpleCacheConfig.CacheSettings.CacheTopology.Topology.Equals("local-cache"))
                        _SimpleCacheConfig.CacheSettings.InProc = true;


                    if (_SimpleCacheConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        _SimpleCacheConfig.CacheSettings.CacheTopology.ClusterSettings = new Alachisoft.NCache.Config.NewDom.Cluster();
                        _SimpleCacheConfig.CacheSettings.CacheTopology.ClusterSettings.Channel = new Alachisoft.NCache.Config.NewDom.Channel();

                        _SimpleCacheConfig.CacheSettings.CacheTopology.ClusterSettings.Channel.TcpPort = ccParam.ClusterPort;
                        _SimpleCacheConfig.CacheSettings.CacheTopology.ClusterSettings.StatsRepInterval = 600;
                        if (_SimpleCacheConfig.CacheSettings.CacheTopology.Topology == "partitioned-replica")
                            _SimpleCacheConfig.CacheSettings.CacheTopology.ClusterSettings.Channel.PortRange = 2;

                    }

                    if (ccParam.EvictionPolicy != null && ccParam.EvictionPolicy != string.Empty)
                    {
                        _SimpleCacheConfig.CacheSettings.EvictionPolicy.Policy = ccParam.EvictionPolicy;
                        _SimpleCacheConfig.CacheSettings.EvictionPolicy.Enabled = true;
                    }

                    if (ccParam.Ratio != -1)
                    {
                        _SimpleCacheConfig.CacheSettings.EvictionPolicy.EvictionRatio = ccParam.Ratio;
                    }

                    if (ccParam.CleanupInterval != -1)
                    {
                        _SimpleCacheConfig.CacheSettings.Cleanup.Interval = ccParam.CleanupInterval;
                    }

                    if (ccParam.DefaultPriority != null && ccParam.DefaultPriority != string.Empty)
                    {
                        _SimpleCacheConfig.CacheSettings.EvictionPolicy.DefaultPriority = ccParam.DefaultPriority;
                        _SimpleCacheConfig.CacheSettings.EvictionPolicy.Enabled = true;
                    }
                    _cacheConfig = _SimpleCacheConfig;
                }
                try
                {
                    _cacheConfig.CacheSettings.Name = ccParam.CacheId;

                    if (_cacheConfig.CacheSettings.CacheType == "clustered-cache")
                    {
                        if (_cacheConfig.CacheDeployment == null)
                        {
                            _cacheConfig.CacheDeployment = new Alachisoft.NCache.Config.NewDom.CacheDeployment();
                            _cacheConfig.CacheDeployment.Servers = new Alachisoft.NCache.Config.NewDom.ServersNodes();

                        }
                        _cacheConfig.CacheDeployment.Servers.NodesList = GetServers(ccParam.Server);
                    }

                    Dictionary<int, Management.ClientConfiguration.Dom.CacheServer> serverList = new Dictionary<int, Management.ClientConfiguration.Dom.CacheServer>();
                    int serverCount = 0;
                    foreach (Alachisoft.NCache.Config.NewDom.ServerNode node in GetServers(ccParam.Server))
                    {
                        Management.ClientConfiguration.Dom.CacheServer tempServer = new Management.ClientConfiguration.Dom.CacheServer();
                        tempServer.ServerName = node.IP;
                        serverList.Add(serverCount, tempServer);
                        serverCount++;
                    }
                    Management.ClientConfiguration.CacheServerList servers = new Management.ClientConfiguration.CacheServerList(serverList);
                    List<string> serversToUpdate = new List<string>();
                    foreach (Alachisoft.NCache.Config.NewDom.ServerNode node in GetServers(ccParam.Server))
                    {
                        NCache.ServerName = node.IP;

                        Console.WriteLine(AppendBlankLine("\nCreating cache") + " '{0}' on server '{1}' ", _cacheConfig.CacheSettings.Name, NCache.ServerName);
                        try
                        {
                            cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            if (cacheServer != null)
                            {
                                Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(_cacheConfig.CacheSettings.Name);

                                if (serverConfig != null)
                                {
                                    throw new Exception("Specified cache already exists.");
                                   
                                }

                                else if (serverConfig != null && ccParam.IsOverWrite)
                                {
                                    NCache.ServerName = node.IP;

                                    if (serverConfig.CacheDeployment != null)
                                    {
                                        if (serverConfig.CacheDeployment.ClientNodes != null)
                                            _cacheConfig.CacheDeployment.ClientNodes = serverConfig.CacheDeployment.ClientNodes;
                                    }
 
                                }
       
                                cacheServer.RegisterCache(_cacheConfig.CacheSettings.Name, _cacheConfig, "", ccParam.IsOverWrite, ccParam.IsHotApply);
                                cacheServer.UpdateClientServersList(_cacheConfig.CacheSettings.Name, servers, "NCACHE");
                                serversToUpdate.Add(node.IP);

                                Console.WriteLine("Cache '{0}' successfully created on server {1}:{2} .", _cacheConfig.CacheSettings.Name, NCache.ServerName, NCache.Port);
                            }

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                    }
                    Management.Management.Util.ManagementWorkFlow.UpdateServerMappingConfig(serversToUpdate.ToArray());
                    
                }
                catch (Exception ex)
                {
                    throw ex;
                }
                finally
                {
                    if (cacheServer != null)
                        cacheServer.Dispose();
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(AppendBlankLine("Failed") + " to create cache on server '{0}'. ", ccParam.Server);
                Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                LogEvent(ex.Message);
            }
            finally
            {
                NCache.Dispose();
            }
        }
        static public ArrayList GetServers(string servers)
        {
            ArrayList serverList = new ArrayList();
            string[] st = servers.Split(new char[] { ',' });
            for (int i = 0; i < st.Length; i++)
            {
                serverList.Add(new Alachisoft.NCache.Config.NewDom.ServerNode(st[i]));
            }
            return serverList;
        }

        private static string AppendBlankLine(string message)
        {
            string afterAppending = "\n" + message;
            return afterAppending;
        }
    }
}
