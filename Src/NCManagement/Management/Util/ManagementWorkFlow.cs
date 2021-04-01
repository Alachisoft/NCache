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
using Alachisoft.NCache.Management.ServiceControl;
using System.Collections;
using Alachisoft.NCache.Config;

namespace Alachisoft.NCache.Management.Management.Util
{
    public class ManagementWorkFlow
    {
        public static Mapping[] GetUpdatedMappingList(Mapping[] oldMapping, Mapping[] newMapping)
        {
            Dictionary<string, Mapping> updatedMappingDictionary = new Dictionary<string, Mapping>();
            foreach (Mapping mapping in newMapping)
            {
                if (mapping != null)
                {
                    updatedMappingDictionary.Add(mapping.PrivateIP, mapping);
                }
            }
            foreach (Mapping mapping in oldMapping)
            {
                if (mapping != null)
                {
                    if (!(updatedMappingDictionary.ContainsKey(mapping.PrivateIP)))
                    {
                        updatedMappingDictionary.Add(mapping.PrivateIP, mapping);
                    }
                }
            }
            return MappingToArray(updatedMappingDictionary);
        }

        public static Mapping[] MappingToArray(Dictionary<string, Mapping> updatedMappingDictionary)
        {
            if (updatedMappingDictionary != null)
            {
                Mapping[] mappingArray = new Mapping[updatedMappingDictionary.Count];
                int index = 0;
                foreach (Mapping node in updatedMappingDictionary.Values)
                {
                    mappingArray[index] = new Mapping();
                    mappingArray[index] = node;
                    index++;
                }

                return mappingArray;
            }
            else
                return null;
        }

        public static void UpdateServerMappingConfig(string[] nodes)
        {
            UpdateServerMappings(GetServerMappings(nodes), nodes);
        }

        private static MappingConfiguration.Dom.MappingConfiguration GetServerMappings(string[] nodes)
        {
            if (nodes != null)
            {
                List<Mapping> managementIPMapping = new List<Mapping>();
                List<Mapping> clientIPMapping = new List<Mapping>();
                foreach (string node in nodes)
                {
                    try
                    {
                        NCacheRPCService NCache = new NCacheRPCService(node);
                        ICacheServer cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                        Hashtable serverIPMapping = cacheServer.GetServerMappingForConfig();
                        if (serverIPMapping.Contains("management-ip-mapping"))
                        {
                            managementIPMapping.Add((Mapping)serverIPMapping["management-ip-mapping"]);
                        }
                        if (serverIPMapping.Contains("client-ip-mapping"))
                        {
                            clientIPMapping.Add((Mapping)serverIPMapping["client-ip-mapping"]);
                        }
                        cacheServer.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }
                }
                if (managementIPMapping.Count == 0 && clientIPMapping.Count == 0)
                {
                    return null;
                }
                MappingConfiguration.Dom.MappingConfiguration mappingConfiguration = new MappingConfiguration.Dom.MappingConfiguration();
                mappingConfiguration.ManagementIPMapping = new ServerMapping(managementIPMapping.ToArray());
                mappingConfiguration.ClientIPMapping = new ServerMapping(clientIPMapping.ToArray());

                return mappingConfiguration;
            }
            return null;
        }

        private static void UpdateServerMappings(MappingConfiguration.Dom.MappingConfiguration mappingConfiguration,string[] nodes)
        {
            if (nodes != null && mappingConfiguration != null)
            {
                foreach (string node in nodes)
                {
                    try
                    {
                        NCacheRPCService NCache = new NCacheRPCService(node);
                        ICacheServer cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                        cacheServer.UpdateServerMappingConfig(mappingConfiguration);
                        cacheServer.Dispose();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}
