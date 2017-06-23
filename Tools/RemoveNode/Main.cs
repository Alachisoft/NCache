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
using System.Data;
using System.Collections;
using System.Globalization;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections.Generic;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Common;


using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Enum;
using System.Net;
using Alachisoft.NCache.Tools.Common;
using System.Diagnostics;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management.ServiceControl;


namespace Alachisoft.NCache.Tools.RemoveNode
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
				RemoveNodeTool.Run(args);
			}
			catch(Exception e)
			{
				Console.WriteLine(e);
			}
		}
	}

    public class RemoveNodeParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {

        private string _cacheId = string.Empty;
        private int _port = -1;
        private string _server = string.Empty;
        private int _cacheSize = 0;

        public RemoveNodeParam()
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

        [ArgumentAttribute(@"/p", @"/port")]
        public int Port
        {
            get { return _port; }
            set { _port = value; }
        }
    }

	/// <summary>
	/// Summary description for RemoveNodeTool.
	/// </summary>
	sealed class RemoveNodeTool
	{
		/// <summary> NCache service controller. </summary>
		static private NCacheRPCService	ncacheService = new NCacheRPCService("");
		/// <summary> Cache ids specified at the command line. </summary>
        static private string _partId = string.Empty;
        static private RemoveNodeParam cParam = new RemoveNodeParam();

        /// <summary>
		/// Sets the application level parameters to those specified at the command line.
		/// </summary>
		/// <param name="args">array of command line parameters</param>


        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache name not specified.");
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
                ncLog.Source = "NCache:RemoveNode Tool";
                ncLog.WriteEntry(msg, type);
            }
        }


        static public void Run(string[] args)
        {
            try
            {
                object param = new RemoveNodeParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (RemoveNodeParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                if (cParam.Port != -1)
                    ncacheService.Port = cParam.Port;

                if (cParam.Port == -1) ncacheService.Port = ncacheService.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;

                if (cParam.Server != null || cParam.Server != string.Empty)
                {
                    ncacheService.ServerName = cParam.Server;
                }
                else
                    cParam.Server = ncacheService.ServerName;
                
				ICacheServer m = ncacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
				if(m != null)
				{
						try
						{
                            IPAddress address;
                            if (!IPAddress.TryParse(ncacheService.ServerName, out address))
                            {
                                string clusterIp = m.GetClusterIP();
                                if (clusterIp != null && clusterIp != string.Empty)
                                    ncacheService.ServerName = clusterIp;
                            }
                            CacheStatusOnServerContainer isClustered = m.IsClusteredCache(cParam.CacheId);

                            CacheStatusOnServer result = isClustered.cacheStatus;
                            if (result == CacheStatusOnServer.Unregistered)
                                throw new Exception("The requested cache is not registered on the specified server.");
                            else if (result == CacheStatusOnServer.LocalCache)
                                throw new Exception("RemoveNode Tool can be used with clustered caches only.");
                            
							
                            Console.WriteLine("Removing '{0}' from the cache '{1}'.", ncacheService.ServerName.ToLower(), cParam.CacheId);
                            NewCacheRegisterationInfo info = m.GetNewUpdatedCacheConfiguration(cParam.CacheId, null, ncacheService.ServerName, false);//.GetNewUpdatedCacheConfiguration(cParam.CacheId, null, NCache.ServerName, false);

                            m.UnregisterCache(cParam.CacheId, string.Empty,true);
                          
                            foreach (string serverName in info.AffectedNodes)
                            {
                                    ncacheService.ServerName = serverName;
                                    m = ncacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                                    m.RegisterCache(cParam.CacheId, info.UpdatedCacheConfig, null, true, false);

                            }

                            //Remove from client.ncconf
                            Dictionary<string, Dictionary<int, Management.ClientConfiguration.Dom.CacheServer>> serversPriorityList = new Dictionary<string, Dictionary<int, Alachisoft.NCache.Management.ClientConfiguration.Dom.CacheServer>>();
                            
                            try
                            {
                                ArrayList clusterNodes = info.UpdatedCacheConfig.CacheDeployment.Servers.NodesList;
                                ToolServerOperations.ClientConfigUtil _clientConfigUtil = new ToolServerOperations.ClientConfigUtil();
                                
                                foreach (Alachisoft.NCache.Config.NewDom.ServerNode nodei in clusterNodes)
                                {
                                    serversPriorityList.Add(nodei.IP, _clientConfigUtil.GetPrioritizedServerListForClient(nodei.IP, cParam.CacheId, clusterNodes));
                                }
                                foreach (Alachisoft.NCache.Config.NewDom.ServerNode node in clusterNodes)
                                {
                                    ncacheService.ServerName = node.IP;
                                    ICacheServer _cacheServer = ncacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                    Management.ClientConfiguration.CacheServerList _cacheServerList = new Management.ClientConfiguration.CacheServerList(serversPriorityList[node.IP]);
                                    _cacheServer.UpdateClientServersList(cParam.CacheId, _cacheServerList, RtContextValue.NCACHE.ToString());
                                }
                                serversPriorityList.Clear();
                                List<ClientNode> clusterClientNodes = info.UpdatedCacheConfig.CacheDeployment.ClientNodes.NodesList;
                                foreach (Alachisoft.NCache.Config.Dom.ClientNode nodei in clusterClientNodes)
                                {
                                    serversPriorityList.Add(nodei.Name, _clientConfigUtil.GetPrioritizedServerListForClient(nodei.Name, cParam.CacheId, clusterNodes));
                                }
                                foreach (Alachisoft.NCache.Config.Dom.ClientNode node in clusterClientNodes)
                                {
                                    ncacheService.ServerName = node.Name;
                                    ICacheServer _cacheServer = ncacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                    Management.ClientConfiguration.CacheServerList _cacheServerList = new Management.ClientConfiguration.CacheServerList(serversPriorityList[node.Name]);
                                    _cacheServer.UpdateClientServersList(cParam.CacheId, _cacheServerList, node.RuntimeContextString);
                                }
                            }
                            catch (Exception e)
                            {
                            }
                           
                        }
                        catch (Exception e)
						{
							Console.Error.WriteLine("Failed to remove '{0}' from '{1}'. Error: {2} ", ncacheService.ServerName.ToLower(), cParam.CacheId, e.Message);
						}
				}
			}
			catch(Exception e)
			{
				Console.Error.WriteLine("Error : {0}", e.Message);
			}
			finally
			{
				if(ncacheService != null) ncacheService.Dispose();
			}
		}
	}
}
