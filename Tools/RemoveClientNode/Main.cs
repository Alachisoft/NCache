// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Config.Dom;
using System.Collections;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using System.Globalization;
using Alachisoft.NCache.Tools.AddClientNode;
using System.Net;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Tools.Common;
using System.Diagnostics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management.ServiceControl;

namespace Alachisoft.NCache.Tools.RemoveClientNode
{
    class Program
    {
        static void Main(string[] args)
        {
            RemoveClientNodeTool.Run(args);
        }
    }

    public class RemoveClientNodeParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {


        private string _cacheId = string.Empty;
        private string _clientCacheId = string.Empty;
        private int _port = -1;
        private string _server = string.Empty;
        private int _cacheSize = 0;


        public RemoveClientNodeParam()
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
            get { return _clientCacheId; }
            set { _clientCacheId = value; }
        }

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
    }

    sealed class RemoveClientNodeTool
    {
        static private NCacheRPCService NCache = new NCacheRPCService("");
        static private ArrayList currentServerNodes = new ArrayList();
        static private ArrayList currentClientNodes = new ArrayList();
        static private Alachisoft.NCache.Config.NewDom.CacheServerConfig config;
        static private Alachisoft.NCache.Management.ICacheServer cacheServer;
        static private RemoveClientNodeParam cParam = new RemoveClientNodeParam();
     
        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
                return false;
            }
            if (string.IsNullOrEmpty(cParam.ClientNode))
            {
                Console.Error.WriteLine("Error: Client node not specified.");
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
                ncLog.Source = "NCache:RemoveClientNode Tool";
                ncLog.WriteEntry(msg, type);
            }
        }


        static public void Run(string[] args)
        {
            try
            {
                object param = new RemoveClientNodeParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (RemoveClientNodeParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                if (cParam.Port != -1)
                    NCache.Port = cParam.Port;

                if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;


                if (cParam.Server != null || cParam.Server != string.Empty)
                {
                    NCache.ServerName = cParam.Server;
                }

                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                config = cacheServer.GetNewConfiguration(cParam.CacheId);
                string getClusterIp = cacheServer.GetClusterIP();
                if (config == null)
                {
                    Console.Error.WriteLine("Error: The cache doesnot exist.");
                    return;
                }
                if (config.CacheSettings.CacheType == "clustered-cache")
                {
                    
                    Console.WriteLine("Removing client node '{0}' from cache '{1}' on server '{2}:{3}'.",
                            cParam.ClientNode, cParam.CacheId, getClusterIp, NCache.Port);
                    foreach (Address node in config.CacheDeployment.Servers.GetAllConfiguredNodes())
                    {
                        currentServerNodes.Add(node.IpAddress.ToString());
                    }
                }
                else 
                {
                    Console.Error.WriteLine("Error: Client nodes cannot be removed from local caches");
                    return;
                }

                if (UpdateConfigs())
                {
                    Console.WriteLine("Client node '{0}' successfully removed from cache '{1}' on server {2}:{3}.",
                        cParam.ClientNode, cParam.CacheId, getClusterIp, NCache.Port);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
            }
            finally
            {
                if(cacheServer!=null)
                cacheServer.Dispose();
            }

        }

        private static bool UpdateConfigs()
        {
            try
            {
                bool successfullremove = false;
                if (config.CacheDeployment.ClientNodes == null)
                {
                    Console.Error.WriteLine("Error : Client node(s) not found."); 
                    return false;
                }

                if (config.CacheDeployment.ClientNodes.NodesList == null)
                {
                    Console.Error.WriteLine("Error : Client node(s) not found.");
                    return false;
                }

                ClientNode[] existingClientNodes=config.CacheDeployment.ClientNodes.Nodes;
                foreach(ClientNode cNode in existingClientNodes)
                {
                        if (cNode.Name == DnsCache.ResolveName(cParam.ClientNode).ToString())
                        {
                            config.CacheDeployment.ClientNodes.NodesList.Remove(cNode);
                            if (config.CacheDeployment.ClientNodes.NodesList.Count == 0)
                                config.CacheDeployment.ClientNodes = null;
                            successfullremove = true;                          
                    }
                }

                if (!successfullremove)
                {
                    Console.Error.WriteLine("Error : Client node(s) not found.");
                    return false;
                }

                foreach (string node in currentServerNodes)
                { 
                    NCache.ServerName = node;
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    cacheServer.RegisterCache(cParam.CacheId, config, string.Empty, true, true);
                }

                NCache.ServerName = cParam.ClientNode;
                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                cacheServer.RemoveCacheFromClientConfig(cParam.CacheId);
            
                return true;

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: {0}", ex.Message);
                return false;
            }
        }
    }
}
