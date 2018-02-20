// Copyright (c) 2018 Alachisoft
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
// limitations under the License

using System;
using Alachisoft.NCache.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using System.Collections.Generic;
using Alachisoft.NCache.Config.Dom;
using System.Collections;
using Alachisoft.NCache.Common.Monitoring;

namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Provides the public interface for the CacheServer.
    /// </summary>
    public class CacheServerModerator
	{
        static string config="client.ncconf";

        private static string LOCALCACHE = "local-cache";
        private static string CLUSTEREDCACHE = "clustered-cache";                    
        
		public static void StartCache(string cacheId)
		{

            CacheService cacheService= new NCacheRPCService(null);
            ICacheServer cs = null;
            try
			{
				cacheService.ServerName = Environment.MachineName;
				 cs = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
				if (cs != null)
				{
                    cs.StartCache(cacheId);
				}
			}

		
			catch (Exception ex)
			{
				throw new ManagementException(ex.Message);
			}
			finally
			{
                 if (cs != null) cs.Dispose();
                cacheService.Dispose();
			}
		}

		public static void StartCache(string cacheId, string serverName)
		{
            ICacheServer cs = null;
            CacheService cacheService = new NCacheRPCService(null);
            CacheServerConfig config = null;
            try
			{
                
				cacheService.ServerName = serverName;
				cs = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
				if (cs != null)
				{
                    config = cs.GetCacheConfiguration(cacheId);
                    if (config != null)
                    {
                        if (!config.InProc)
                            cs.StartCache(cacheId);
                    }
                    else
                        throw new ManagementException("Unable to Start Cache. Specified cache is not registered.");
                }
			}
			catch (Exception ex)
			{
                if (cs != null)
                {
                    cs.Dispose();
                    cs = null;
                }
				throw new ManagementException(ex.Message);
			}
			finally
			{
                if (cs != null) cs.Dispose();
                cacheService.Dispose();
			}
		}

        public static void StopCache(string cacheId)
		{
            ICacheServer cs = null;
            CacheService cacheService = new NCacheRPCService(null);
            CacheServerConfig config = null;
            try
			{
				cacheService.ServerName = Environment.MachineName;
				 cs = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
				if (cs != null)
				{
                    config = cs.GetCacheConfiguration(cacheId);
                    if (!config.InProc)
                        cs.StopCache(cacheId);
                    else
                        throw new ManagementException("Inproc caches cannot be stopped explicitly.");
				}
			}
            finally
			{
                if (cs != null) cs.Dispose();
                cacheService.Dispose();
			}
		}

        public static void StopCache(string cacheId, string serverName, bool isGracefulShutdown)
		{
            ICacheServer cs = null;
            CacheService cacheService = new NCacheRPCService(null);
            try
			{
               
				cacheService.ServerName = serverName;
				 cs = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
				if (cs != null)
				{

					cs.StopCache(cacheId);

				}
			}	
            catch (Exception ex)
			{
                if (cs != null)
                {
                    cs.Dispose();
                    cs = null;
                }
				throw new ManagementException(ex.Message);
			}
			finally
			{
                if (cs != null) cs.Dispose();
                cacheService.Dispose();
			}
		}

        public static  Dictionary<Runtime.CacheManagement.ServerNode, List<Runtime.CacheManagement.CacheClient>> GetCacheClients(string cacheName, string initialNodeName, Runtime.CacheManagement.CacheContext context, int port)
        {
            CacheService cacheService = GetCacheService(context);
            if (port != 0)
                cacheService.Port = port;
            string startingNode = initialNodeName;
            Alachisoft.NCache.Config.Dom.CacheServerConfig cacheServerConfig = null;
            Alachisoft.NCache.Management.ICacheServer cacheServer = null;
            Dictionary<Runtime.CacheManagement.ServerNode, List<Runtime.CacheManagement.CacheClient>> clientList = new Dictionary<Runtime.CacheManagement.ServerNode, List<Runtime.CacheManagement.CacheClient>>();

            try
            {
                if (initialNodeName.Equals(string.Empty))
                {
                    cacheServerConfig = GetCacheConfigThroughClientConfig(cacheName,port,context);

                    if (cacheServerConfig == null)
                        throw new ManagementException("cache with name " + cacheName + " not found in " + config);
                }
                else
                {
                    cacheService.ServerName = initialNodeName;
                    cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    if (cacheServer == null)
                        throw new ManagementException("provided initial node not available");

                    cacheServerConfig = cacheServer.GetCacheConfiguration(cacheName);
                    if (cacheServerConfig == null)
                        throw new ManagementException("cache with name " + cacheName + " not registered on specified node");
                }                        

                //For Local Cache
                if (cacheServerConfig.CacheType.Equals(LOCALCACHE, StringComparison.OrdinalIgnoreCase))
                {
                    if (cacheServerConfig.InProc)
                        throw new ArgumentException("API is not supported for Local Inproc Cache");
                    
                    cacheService.ServerName = Environment.MachineName;
                    cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                    if (cacheServer != null)
                    {

                        if (cacheServer.IsRunning(cacheName))
                        {
                            Runtime.CacheManagement.ServerNode serverNode = new Runtime.CacheManagement.ServerNode();
                            serverNode.ServerIP = Environment.MachineName;

                            List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> clients = cacheServer.GetClientProcessStats(cacheServerConfig.Name);
                            List<Runtime.CacheManagement.CacheClient> list = new List<Runtime.CacheManagement.CacheClient>();
                            foreach (Alachisoft.NCache.Common.Monitoring.ClientProcessStats clientNode in clients)
                            {
                                Runtime.CacheManagement.CacheClient cacheClient = new Runtime.CacheManagement.CacheClient();
                                cacheClient.ClientIP = clientNode.Address.IpAddress.ToString();
                                cacheClient.Port = clientNode.Address.Port;
                                cacheClient.ProcessID = clientNode.ProcessID;
                                list.Add(cacheClient);
                            }
                            clientList.Add(serverNode, list);
                        }

                    }
                    return clientList;
                    
                }
                //For Clustered Cache
                else
                {                                       
                    ArrayList initialHost = InitialHostList(cacheServerConfig.Cluster.Channel.InitialHosts);
                    foreach (object host in initialHost)
                    {
                        try
                        {
                            cacheService.ServerName = (string)host;
                            cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            if (cacheServer.IsRunning(cacheName))
                            {
                                Runtime.CacheManagement.ServerNode serverNode = new Runtime.CacheManagement.ServerNode();
                                serverNode.ServerIP = host as string;
                                serverNode.Port = cacheServerConfig.Cluster.Channel.TcpPort;

                                List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> clients = cacheServer.GetClientProcessStats(cacheServerConfig.Name);
                                List<Runtime.CacheManagement.CacheClient> list = new List<Runtime.CacheManagement.CacheClient>();
                                foreach (Alachisoft.NCache.Common.Monitoring.ClientProcessStats clientNode in clients)
                                {
                                    Runtime.CacheManagement.CacheClient cacheClient = new Runtime.CacheManagement.CacheClient();
                                    cacheClient.ClientIP = clientNode.Address.IpAddress.ToString();
                                    cacheClient.Port = clientNode.Address.Port;
                                    cacheClient.ProcessID = clientNode.ProcessID;
                                    list.Add(cacheClient);                                    
                                }
                                clientList.Add(serverNode, list);                                
                            }

                        }
                        catch (Exception e)
                        {
                           
                        }
                    }

                    return clientList;
                }
            }
            catch (Exception ex)
            {
                throw new ManagementException(ex.Message);
            }
            finally
            {
                if (cacheServer != null) cacheServer.Dispose();
                cacheService.Dispose();
            }
            return null;
        }


        private static CacheService GetCacheService(Runtime.CacheManagement.CacheContext context) 
        {
            switch (context) 
            {
                case Runtime.CacheManagement.CacheContext.NCache: return new NCacheRPCService(null);                             
            }
            return null;
        }

        private static ArrayList InitialHostList(string initialHostsColl)
        {
            ArrayList list = new ArrayList(5);
            string[] commaSplit = initialHostsColl.Split(',');
            foreach (string initialHost in commaSplit)
            {
                string[] split = initialHost.Split('[', ']');
                list.Add(split[0]);
            }
            return list;
        }

        private static Runtime.CacheManagement.CacheTopology GetCacheTopology(string topologyType)
        {
            switch (topologyType)
            { 
                case "replicated":
                case "replicated-server": return Runtime.CacheManagement.CacheTopology.Replicated;
                case "partitioned":
                case "partitioned-server": return Runtime.CacheManagement.CacheTopology.Partitioned;
                default: return Runtime.CacheManagement.CacheTopology.None;
            }
        }

        private static CacheServerConfig GetCacheConfigThroughClientConfig(string cacheName, int port, Runtime.CacheManagement.CacheContext context)
        {
            CacheServerConfig cacheServerConfig = null;
            ClientConfiguration.Dom.CacheServer[] serverNodes = null;
            ICacheServer cacheServer = null;

            CacheService cacheService = GetCacheService(context);
            
            if (port != 0)
                cacheService.Port = port;

            try
            {
                //Get Server Info from Client.nconf for specified cacheName
                ClientConfiguration.Dom.ClientConfiguration clientConfiguration = ClientConfiguration.ClientConfigManager.GetClientConfiguration(cacheName);
                if (clientConfiguration != null)
                {
                    Dictionary<string, CacheConfiguration> cacheConfigurationMap = clientConfiguration.CacheConfigurationsMap;
                    CacheConfiguration cacheClientConfiguration = null;

                    try
                    {
                        cacheClientConfiguration = cacheConfigurationMap[cacheName];
                    }
                    catch (System.Collections.Generic.KeyNotFoundException ex)
                    {            
                    }

                    if (cacheClientConfiguration == null)
                        throw new ManagementException("cache not found in " + config);

                    serverNodes = cacheClientConfiguration.Servers;

                    foreach (ClientConfiguration.Dom.CacheServer node in serverNodes)
                    {
                        try
                        {
                            cacheService.ServerName = node.ServerName;
                            cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            if (cacheServer != null)
                            {
                                cacheServerConfig = cacheServer.GetCacheConfiguration(cacheName);
                                //dispose the server
                                cacheServer.Dispose();
                                cacheServer = null;

                                if (cacheServerConfig != null)
                                    break;
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                else
                {
                    throw new ManagementException("error while fetching info from " + config);
                }
            }
            finally
            {
                if ( cacheServer !=null ) cacheServer.Dispose();
                if( cacheService !=null) cacheService.Dispose();
            }
            return cacheServerConfig;

        }

        public static Dictionary<string, TopicStats> GetTopicStat(string cacheName, string initialNodeName, Runtime.CacheManagement.CacheContext context, int port)
        {
            CacheService cacheService = GetCacheService(context);
            if (port != 0)
                cacheService.Port = port;
            string startingNode = initialNodeName;
            CacheServerConfig cacheServerConfig = null;
            ICacheServer cacheServer = null;
            


            try
            {
                if (initialNodeName.Equals(string.Empty))
                {
                    cacheServerConfig = GetCacheConfigThroughClientConfig(cacheName, port, context);
                    if (cacheServerConfig == null)
                        throw new ManagementException("cache with name " + cacheName + " not found in " + config);
                }
                else
                {
                    cacheService.ServerName = initialNodeName;
                    cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                    if (cacheServer == null)
                        throw new ManagementException("provided initial node not available");

                    cacheServerConfig = cacheServer.GetCacheConfiguration(cacheName);
                    if (cacheServerConfig == null)
                        throw new ManagementException("cache with name " + cacheName + " not registered on specified node");
                }                      

                //For Local Cache
                if (cacheServerConfig.CacheType.Equals(LOCALCACHE, StringComparison.OrdinalIgnoreCase))
                {
                    if (cacheServerConfig.InProc)
                        throw new ArgumentException("API is not supported for Local Inproc Cache");

                    cacheService.ServerName = Environment.MachineName;
                    cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                    if (cacheServer != null && cacheServer.IsRunning(cacheName))
                    {
                        return cacheServer.GetTopicStats(cacheServerConfig.Name);
                    }
                }
                //For Clustered Cache
                else
                {
                    Dictionary<string, TopicStats> topicWiseStat = new Dictionary<string, TopicStats>();
                    ArrayList initialHost = InitialHostList(cacheServerConfig.Cluster.Channel.InitialHosts);
                    foreach (object host in initialHost)
                    {
                        try
                        {
                            cacheService.ServerName = (string)host;
                            cacheServer = cacheService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                            if (cacheServer.IsRunning(cacheName))
                            {
                                Dictionary<string, TopicStats> NodeWisetopicStat = cacheServer.GetTopicStats(cacheServerConfig.Name);
                                if (NodeWisetopicStat != null)
                                {
                                    foreach (var item in NodeWisetopicStat)
                                    {
                                        if (!topicWiseStat.ContainsKey(item.Key))
                                        {
                                            item.Value.TopicName = item.Key;
                                            topicWiseStat.Add(item.Key, ((TopicStats)item.Value.Clone()));
                                        }
                                        else
                                        {
                                            TopicStats topicStat = topicWiseStat[item.Key];
                                            topicStat.CurrentMessageCount += item.Value.CurrentMessageCount;

                                        }
                                    }
                                }
                            }

                        }
                        catch (Exception e)
                        {
                        }
                    }
                    return topicWiseStat;

                }
            }
            catch (Exception ex)
            {
                throw new ManagementException(ex.Message);
            }
            finally
            {
                if (cacheServer != null) cacheServer.Dispose();
                cacheService.Dispose();
            }
            return null;
        }
    }
}
