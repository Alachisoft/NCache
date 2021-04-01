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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Exceptions;
using System.IO;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config;

namespace Alachisoft.NCache.Management.MappingConfiguration
{
    public class MappingConfigurationManager
    {
        static string m_configDir = DIRNAME;
        static string m_configFileName = FILENAME;
        static string DIRNAME = "Config";
        static string FILENAME = "server-end-point.ncconf";
        private static object _syncRoot = new object();

        private static void CombinePath()
        {
            m_configDir = AppUtil.InstallDir;

            if (m_configDir == null || m_configDir.Length == 0)
            {
                throw new ManagementException("Missing installation folder information");
            }

            m_configDir = Path.Combine(m_configDir, DIRNAME);
            if (!Directory.Exists(m_configDir))
                Directory.CreateDirectory(m_configDir);

            m_configFileName = Path.Combine(m_configDir, FILENAME);
        }

        private static MappingConfiguration.Dom.MappingConfiguration LoadXml()
        {
            MappingConfiguration.Dom.MappingConfiguration config= null;
            lock (_syncRoot)
            {
                if (String.IsNullOrEmpty(m_configFileName))
                    CombinePath();
                try
                {
                    ConfigurationBuilder configBuilder = new ConfigurationBuilder(m_configFileName);
                    configBuilder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Management.MappingConfiguration.Dom.MappingConfiguration));
                    configBuilder.ReadConfiguration();

                    MappingConfiguration.Dom.MappingConfiguration mappingConfiguration = null;
                    Object[] configuration = configBuilder.Configuration;

                    if (configuration != null && configuration.Length > 0)
                    {
                        for (int i = 0; i < configuration.Length; i++)
                        {
                            mappingConfiguration = configuration[i] as MappingConfiguration.Dom.MappingConfiguration;
                            break;
                        }
                    }

                    config = mappingConfiguration;
                }
                catch (Exception)
                { }

                if (config == null)
                    config = new Alachisoft.NCache.Management.MappingConfiguration.Dom.MappingConfiguration();

                if (config.ManagementIPMapping == null)
                    config.ManagementIPMapping = new ServerMapping();

                if (config.ClientIPMapping == null)
                    config.ClientIPMapping = new ServerMapping();
            }
            return config;
        }

        private static string ToXml(MappingConfiguration.Dom.MappingConfiguration config)
        {
            StringBuilder sb = new StringBuilder();
            object[] configuration = new object[1];
            configuration[0] = config;
            ConfigurationBuilder cfgBuilder = new ConfigurationBuilder(configuration);
            cfgBuilder.RegisterRootConfigurationObject(typeof(ClientConfiguration.Dom.ClientConfiguration));
            sb.Append(cfgBuilder.GetXmlString());
            return sb.ToString();
        }

        private static void SaveConfiguration(MappingConfiguration.Dom.MappingConfiguration config)
        {
            lock (_syncRoot)
            {
                if (m_configFileName == null || m_configFileName == "")
                    CombinePath();

                FileStream fs = null;
                StreamWriter sw = null;

                try
                {
                    fs = new FileStream(m_configFileName, FileMode.Create);
                    sw = new StreamWriter(fs);
                    sw.Write(ToXml(config));
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

        public static void UpdateMappingConfiguration(MappingConfiguration.Dom.MappingConfiguration configuration)
        {
            MappingConfiguration.Dom.MappingConfiguration config = LoadXml();
            if(configuration.ManagementIPMapping != null)
            {
                config.ManagementIPMapping = new ServerMapping(Management.Util.ManagementWorkFlow.GetUpdatedMappingList(config.ManagementIPMapping.MappingServers, configuration.ManagementIPMapping.MappingServers));
            }
            if (configuration.ClientIPMapping != null)
            {
                config.ClientIPMapping = new ServerMapping(Management.Util.ManagementWorkFlow.GetUpdatedMappingList(config.ClientIPMapping.MappingServers, configuration.ClientIPMapping.MappingServers));
            }
            SaveConfiguration(config);
        }

        public static MappingConfiguration.Dom.MappingConfiguration GetMappingConfiguration()
        {
            MappingConfiguration.Dom.MappingConfiguration config = LoadXml();
            return config;
        }
    }
}
