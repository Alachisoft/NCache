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
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Alachisoft.NCache.Management.Util
{
   public class ConfigurationUtil
    {
        static object _lock = new object();
       
        static IDictionary<string, ArrayList> _appliedConfiguredNodes = new Dictionary<string, ArrayList>();
        static IDictionary<string, ArrayList> _appliedbridgeNodes = new Dictionary<string, ArrayList>();
        
     public static void AddConfiguredNodes (string cacheId, string node)
        {
            lock (_lock)
            {
                if (_appliedConfiguredNodes.ContainsKey(cacheId))
                {
                    if (!_appliedConfiguredNodes[cacheId].Contains(node))
                          _appliedConfiguredNodes[cacheId].Add(node);

                }
                else
                {
                    ArrayList list = new ArrayList();
                    list.Add(node);
                    _appliedConfiguredNodes[cacheId] = list;
                }
            }
        }

        public static ArrayList GetConfigureNodes (string instance ,bool isbridge)
        {
            try
            {
                ArrayList retList;
                if (!isbridge)
                    _appliedConfiguredNodes.TryGetValue(instance, out retList);
                else
                    _appliedbridgeNodes.TryGetValue(instance, out retList);
                return retList;
            }
            catch (Exception ex)
            {
                return null;

            }
        }

        public static void RemoveClusterFromAppliedConfig (string clusterId)
        {
            if (!string.IsNullOrEmpty (clusterId))
            {
                lock (_lock)
                {
                    if (_appliedConfiguredNodes.ContainsKey(clusterId))
                        _appliedConfiguredNodes.Remove(clusterId);
                }
            }
        }

        public static ArrayList GetAvailibleLatestServers (string instanceName, object localConfig, bool isBridge)
        {
            if (!string.IsNullOrEmpty(instanceName))
            {
                ArrayList misamtchServers =null;
                ArrayList localServers = null;
                
                    if (_appliedConfiguredNodes.ContainsKey(instanceName))
                    {
                        misamtchServers = GetConfigureNodes(instanceName, isBridge);
                    }
                    localServers = FromHostListToServers((localConfig as CacheServerConfig).Cluster.Channel.InitialHosts);
               
                ArrayList clusterServers = new ArrayList();
                if (localServers.Count>0)
                {
                    if (misamtchServers != null && misamtchServers.Count > 0)
                    {
                        foreach (string server in misamtchServers)
                        {
                            if (!localServers.Contains(server))
                            {
                                clusterServers.Add(server);
                            }
                        }
                    }
                    else
                        clusterServers = localServers;
                    return clusterServers;
                }
                return null;
               
            }
            else
                return null;

        }

        public static string GetLatestDeploymentConfiguration(IDictionary<string, ConfigurationVersion> configs)
        {

            if (configs != null && configs.Count > 0)
            {
                string latestserver = null;
                ConfigurationVersion latestConfig = null;
                double number = 0;
                foreach (KeyValuePair<string, ConfigurationVersion> config in configs)
                {
                    latestConfig = config.Value as ConfigurationVersion;
                    if (latestConfig.DeploymentVersion > number)
                    {
                        number = latestConfig.DeploymentVersion;
                        latestserver = config.Key.ToString();
                    }
                }
                return latestserver;
            }
               
            return null;
        }

        public static string GetLatestSettingsConfiguration(IDictionary<string, ConfigurationVersion> configs)
        {
            if (configs != null && configs.Count > 0)
            {
                string latestserver = null;
                ConfigurationVersion latestConfig = null;
                double number = 0;
                foreach (KeyValuePair<string, ConfigurationVersion> config in configs)
                {
                    latestConfig = config.Value as ConfigurationVersion;
                    if (latestConfig.ConfigVersion > number)
                    {
                        number = latestConfig.ConfigVersion;
                        latestserver = config.Key.ToString();
                    }
                }
                return latestserver;
            }

            return null;
        }


        public static double GetLatestConfigurationversion(IDictionary<string, ConfigurationVersion> configs)
        {
            if (configs != null && configs.Count > 0)
            {
                ConfigurationVersion latestConfig = null;
                double number = 0;
                foreach (KeyValuePair<string, ConfigurationVersion> config in configs)
                {
                    latestConfig = config.Value as ConfigurationVersion;
                    if (latestConfig.ConfigVersion > number)
                    {
                        number = latestConfig.ConfigVersion;
                       
                    }
                }
                return number;
            }

            return 0;
        }

        public static bool CacheExists(IDictionary<string, ConfigurationVersion> configs)
        {
            if (configs != null && configs.Count > 0)
            {
                int failedCount = 0;
                ConfigurationVersion latestConfig = null;
                foreach (KeyValuePair<string, ConfigurationVersion> config in configs)
                {
                    latestConfig = config.Value as ConfigurationVersion;
                    if (string.IsNullOrEmpty(latestConfig.ConfigID))
                    {
                        failedCount++;

                    }
                }
                if (failedCount < configs.Count)
                    return true;
                else
                    return false;
            }

            return false;
        }
        
        public static ArrayList FromHostListToServers(string hostString)
        {
            ArrayList servers = new ArrayList();

            if (string.IsNullOrEmpty(hostString))
                return servers;

            if (hostString.IndexOf(',') != -1)
            {
                string[] hosts = hostString.Split(new char[] { ',' });
                if (hosts != null)
                {
                    for (int i = 0; i < hosts.Length; i++)
                    {
                        hosts[i] = hosts[i].Trim();
                        servers.Add(hosts[i].Substring(0, hosts[i].IndexOf('[')));
                    }
                }
            }
            else
            {
                servers.Add(hostString.Trim().Substring(0, hostString.IndexOf('[')));
            }
            return servers;
        }

    }
}
