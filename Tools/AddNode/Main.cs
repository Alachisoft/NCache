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
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Xml;
using System.Net;
using Microsoft.Win32;

using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Enum;

using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Runtime.Serialization.IO;
using System.Collections.Generic;
using Alachisoft.NCache.Tools.Common;
using System.Diagnostics;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management.ServiceControl;



namespace Alachisoft.NCache.Tools.AddNode
{
    /// <summary>
    /// Main application class
    /// </summary>
    class Application
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AddNodeTool.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    public class AddNodeParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        
       
        private string _cacheId = string.Empty;
        private int _port = -1;
        private string _source = string.Empty;
        private string _newServer = string.Empty;

        public AddNodeParam()
        {
        }

        [ArgumentAttribute("", "")]
        public string CacheId
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [ArgumentAttribute(@"/x", @"/existing")]
        public string ExistingServer
        {
            get { return _source; }
            set { _source = value; }
        }

        [ArgumentAttribute(@"/N", @"/new-server")]
        public string NewServer
        {
            get { return _newServer; }
            set { _newServer = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
    }

    /// <summary>
    /// Summary description for AddNodeTool.
    /// </summary>
    sealed class AddNodeTool
    {
        /// <summary> NCache service controller. </summary>
        static private NCacheRPCService NCache = new NCacheRPCService("");
        /// <summary> Re-register flag specified at the command line. </summary>
        static private bool reregister = true;
        /// <summary> Configuration ids specified at the command line. </summary>
        static private ArrayList s_configId = new ArrayList();
        /// <summary>Get the path of config file from Windows Registry.</summary>
        static private RegistryKey r = Registry.LocalMachine.OpenSubKey(@"Software\Alachisoft\NCache");
        /// <summary> Configuration filename specified at the command line. </summary>
        static private string s_fileName = (string)r.GetValue("InstallDir") + @"config\config.ncconf";
        /// <summary> Partition name specified at the command line. </summary>
        static private string _partId = string.Empty;
        static private AddNodeParam cParam = new AddNodeParam();

        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
                return false;
            }
                           
            if (string.IsNullOrEmpty(cParam.NewServer))
            {
                Console.Error.WriteLine("Error: New server is not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(cParam.ExistingServer))
            {
                Console.Error.WriteLine("Error: Existing server is not specified.");
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
                ncLog.Source = "NCache:AddNode Tool";
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
                object param = new AddNodeParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (AddNodeParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }
               
                if (!ValidateParameters()) return;
                if (cParam.Port != -1)
                {
                    NCache.Port = cParam.Port;
                }
                if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                
                    try
                    {
                        if (cParam.ExistingServer != null || cParam.ExistingServer != string.Empty)
                        {
                            NCache.ServerName = cParam.ExistingServer;
                        }
                        IPAddress address;
                        string clusterIp;

                        ICacheServer m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                        if (!IPAddress.TryParse(cParam.NewServer, out address))
                        {
                            clusterIp = m.GetClusterIP();
                            if (clusterIp != null && clusterIp != string.Empty)
                                cParam.NewServer = clusterIp;
                        }

                        NCache.ServerName = cParam.ExistingServer;
                        m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                        if (!IPAddress.TryParse(cParam.ExistingServer, out address))
                        {
                            clusterIp = m.GetClusterIP();
                            if (clusterIp != null && clusterIp != string.Empty)
                                cParam.ExistingServer = clusterIp;
                        }
                        CacheStatusOnServerContainer isClustered = m.IsClusteredCache(cParam.CacheId);
                        CacheStatusOnServer result = isClustered.cacheStatus;                       
                        
                        if(result == CacheStatusOnServer.Unregistered)
                            throw new Exception("The requested cache is not registered on the source node.");
                        else if(result == CacheStatusOnServer.LocalCache)
                            throw new Exception("The AddNode Tool can be used with clustered caches only");

                        Console.WriteLine("Adding node '{0}' to cache '{1}'.", cParam.NewServer, cParam.CacheId);

                        NewCacheRegisterationInfo info = m.GetNewUpdatedCacheConfiguration(cParam.CacheId, _partId, cParam.NewServer, true);

                        //muds:
                        //first of all try to register the cache on the destination server.
                        try
                        {
                            NCache.ServerName = cParam.NewServer;
                            m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                            if (!IPAddress.TryParse(cParam.NewServer, out address))
                            {
                                clusterIp = m.GetClusterIP();
                                if (clusterIp != null && clusterIp != string.Empty)
                                    cParam.NewServer = clusterIp;
                            }

                            m.RegisterCache(cParam.CacheId, info.UpdatedCacheConfig, _partId, true, false);
                            
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine("Failed to Create Cache on '{0}'. ", NCache.ServerName);
                            Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);
                           
                            LogEvent(ex.Message);
                            NCache.Dispose();
                            return;
                        }
                        finally
                        {
                            m.Dispose();
                        }
                        //muds:
                        // Now update the cache configurations on all the servers where the cache
                        //is registered.
                        foreach (string serverName in info.AffectedNodes)
                        {
                            if (info.AffectedPartitions.Count > 0)
                            {
                                foreach (string partId in info.AffectedPartitions)
                                {
                                    try
                                    {
                                        NCache.ServerName = serverName;
                                        if (!IPAddress.TryParse(NCache.ServerName, out address))
                                        {
                                            clusterIp = m.GetClusterIP();
                                            if (clusterIp != null && clusterIp != string.Empty)
                                                NCache.ServerName = clusterIp;
                                        }
                                        reregister = true;
                                        m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
        
                                        m.RegisterCache(cParam.CacheId, info.UpdatedCacheConfig, _partId, true, false);


                                    }
                                    catch (Exception ex)
                                    {
                                        Console.Error.WriteLine("Failed to Create Cache on '{0}'. ", NCache.ServerName);
                                        Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);

                                        LogEvent(ex.Message);
                                    }
                                    finally
                                    {
                                        m.Dispose();
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
                                        clusterIp = m.GetClusterIP();
                                        if (clusterIp != null && clusterIp != string.Empty)
                                            NCache.ServerName = clusterIp;
                                    }
                                    reregister = true;
                                    m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                    m.RegisterCache(cParam.CacheId, info.UpdatedCacheConfig, _partId, true, false);

                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine("Failed to Create Cache on '{0}'. ", NCache.ServerName);
                                    Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);

                                    LogEvent(ex.Message);
                                    NCache.Dispose();
                                    return;
                                }
                                finally
                                {
                                    m.Dispose();
                                }
                            }
                        }
                        List<string> serversToUpdate = new List<string>();
                        foreach (Config.NewDom.ServerNode node in info.UpdatedCacheConfig.CacheDeployment.Servers.ServerNodeList)
                        {
                            serversToUpdate.Add(node.IP);
                        }
                        Management.Management.Util.ManagementWorkFlow.UpdateServerMappingConfig(serversToUpdate.ToArray());
                        //Add in client.ncconf
                        Dictionary<string, Dictionary<int, Management.ClientConfiguration.Dom.CacheServer>> serversPriorityList = new Dictionary<string, Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer>>();
                        ToolServerOperations.ClientConfigUtil _clientConfigUtil = new ToolServerOperations.ClientConfigUtil();
                        try
                        {                            
                            ArrayList clusterNodes = info.UpdatedCacheConfig.CacheDeployment.Servers.NodesList;
                            
                            foreach (Alachisoft.NCache.Config.NewDom.ServerNode nodei in clusterNodes)
                            {
                                serversPriorityList.Add(nodei.IP, _clientConfigUtil.GetPrioritizedServerListForClient(nodei.IP, cParam.CacheId, clusterNodes));
                            }

                            Alachisoft.NCache.Config.NewDom.ServerNode nodeForClientList= new Config.NewDom.ServerNode();//Hack: priority list requires a serverIP and client Ip cant be used hence!!
                            foreach (Alachisoft.NCache.Config.NewDom.ServerNode node in clusterNodes)
                            {
                                NCache.ServerName = node.IP;
                                ICacheServer _cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                Management.ClientConfiguration.CacheServerList _cacheServerList = new Management.ClientConfiguration.CacheServerList(serversPriorityList[node.IP]);
                                _cacheServer.UpdateClientServersList(cParam.CacheId, _cacheServerList, RtContextValue.NCACHE.ToString());
                                nodeForClientList = node;
                            }
                           

                            List<Alachisoft.NCache.Config.Dom.ClientNode> clientNodeList = info.UpdatedCacheConfig.CacheDeployment.ClientNodes.NodesList;
                            foreach (Alachisoft.NCache.Config.Dom.ClientNode node in clientNodeList)
                            {
                                NCache.ServerName = node.Name;
                                ICacheServer _cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                Management.ClientConfiguration.CacheServerList _cacheServerList = new Management.ClientConfiguration.CacheServerList(serversPriorityList[nodeForClientList.IP]);
                                _cacheServer.UpdateClientServersList(cParam.CacheId, _cacheServerList, RtContextValue.NCACHE.ToString());
                            }
                        }
                        catch (Exception e)
                        {
                        }

                        Console.WriteLine("'{0}' successfully added to cache '{1}'.\n", cParam.NewServer,
                                cParam.CacheId);
                    }

                    catch (ConfigurationException e)
                    {
                        Console.Error.WriteLine("Failed to add '{0}' to '{1}'. Error: {2} ", NCache.ServerName.ToLower(), cParam.CacheId, e.Message);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine("Failed to add '{0}' to '{1}'. Error: {2} ", NCache.ServerName.ToLower(), cParam.CacheId, e.Message);
                    }                    
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : {0}", e.Message);
            }
            finally
            {
                NCache.Dispose();
            }
        }
    }
}
