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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;


namespace Alachisoft.NCache.Management
{
    internal class CachePortsConfig
    {
        private const string _configName = "CacheHostPorts.xml";
        private string _installPath;
        private string _filePath;
        private Hashtable _configTable;

        public CachePortsConfig()
        {
            _installPath = Path.Combine(AppUtil.InstallDir,"bin", "service") ;
            if (!Directory.Exists(_installPath))
                _installPath = Environment.CurrentDirectory;
            _filePath = Path.Combine(_installPath, _configName);
            Intialize();
        }

        public void Intialize ()
        {
            try
            {
                if (!File.Exists(_filePath))
                    File.Create(_filePath).Close();
                _configTable = ReadConfiguration();
            }
            catch (Exception ex)
            {

            }
        }

        public void InitializeConfigTableAtStart(Hashtable config)
        {
            if (config != null)
                _configTable = config;
        }

        internal void WriteConfiguration(Hashtable cachePorts)
        {
            try
            {
                SaveConfiguration(ConvertToDom(cachePorts));
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while writing Config file: ", EventLogEntryType.Error);
            }
        }

        internal Hashtable ReadConfiguration ()
        {
            try
            {
                return LoadConfig();
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while reading configuration from file: ", EventLogEntryType.Error);
                return new Hashtable();
            }
        }

        internal void AddConfiguration(string cacheId, int port)
        {
            try
            {
                if (!_configTable.ContainsKey(cacheId))
                    _configTable.Add(cacheId.ToLower(), port);
                else
                    _configTable[cacheId.ToLower()] = port;
                SaveConfiguration(ConvertToDom(_configTable));

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while editing Config file: " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        internal void RemoveConfiguration (string cacheId)
        {
            try
            {
                _configTable.Remove(cacheId.ToLower());
                SaveConfiguration(ConvertToDom(_configTable));

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent("Exception occured while removing Config file: " + ex.ToString(), EventLogEntryType.Error);
            }
        }

        CachePortsConfigParameters [] ConvertToDom (Hashtable  config)
        {
            CachePortsConfigParameters [] newConfig = new CachePortsConfigParameters[config.Count];
            IDictionaryEnumerator enm = config.GetEnumerator();
            int counter = 0;
            while (enm.MoveNext())
            {
                CachePortsConfigParameters configParam = new CachePortsConfigParameters();
                configParam.CacheId = enm.Key.ToString().ToLower();
                configParam.MangementPort = Convert.ToInt32( enm.Value);
                newConfig[counter] = configParam;
                counter++;
            }
            return newConfig;
        }

        private Hashtable LoadConfig()
        {
            try {
                ConfigurationBuilder builder = new ConfigurationBuilder(_filePath);
                builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.Dom.CachePortsConfigParameters));
                builder.ReadConfiguration();
                CachePortsConfigParameters[] newConfiguration = new CachePortsConfigParameters[builder.Configuration.Length];
                builder.Configuration.CopyTo(newConfiguration, 0);
                return ToHashtable(newConfiguration);
            }
            catch (Exception ex)
            {
                return new Hashtable();

            }
        }

        Hashtable ToHashtable (CachePortsConfigParameters [] newCaches)
        {
            Hashtable config = new Hashtable();
            for (int i =0; i<newCaches.Length; i++)
            {
                config.Add(newCaches[i].CacheId.ToLower(), newCaches[i].MangementPort);
            }
            return config;
        }

        private void SaveConfiguration(object[] configuration)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<cache-ports>\r\n");
            if (configuration != null && configuration.Length > 0)
            {
                ConfigurationBuilder builder = new ConfigurationBuilder(configuration);
                builder.RegisterRootConfigurationObject(typeof(CachePortsConfigParameters));
                xml.Append(builder.GetXmlString());
            }
            xml.Append("\r\n</cache-ports>");
            WriteXmlToFile(xml.ToString());
        }

        private void WriteXmlToFile(string xml)
        {
            if (!File.Exists(_filePath))
            {
                File.Create(_filePath);
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(_filePath, FileMode.Create);
                sw = new StreamWriter(fs);

                sw.Write(xml);
                sw.Flush();
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
            finally
            {
                if (sw != null)
                {
                    try
                    {
                        sw.Close();
                    }
                    catch (Exception)
                    {
                    }
                    sw.Dispose();
                    sw = null;
                }
                if (fs != null)
                {
                    try
                    {
                        fs.Close();
                    }
                    catch (Exception)
                    {
                    }
                    fs.Dispose();
                    fs = null;
                }
            }
        }
    }
}
