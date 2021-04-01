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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Config;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Management
{
    /// <summary>
    /// Helps locate the configuration information file.
    /// </summary>
    public class CacheConfigManager
    {
        /// <summary> Default NCache tcp channel port. </summary>
        public const int NCACHE_DEF_TCP_PORT = 8250;
        /// <summary> Default JvCache tcp channel port. </summary>
        public const int JVCACHE_DEF_TCP_PORT = 8270;
        /// <summary> Default http channel port. </summary>
        public const int DEF_HTTP_PORT = 8251;
        /// <summary> Default IPC channel port. </summary>
        public const string DEF_IPC_PORT_NAME = "ncCacheHost";
        /// <summary>Configuration file folder name</summary>
        private const string DIRNAME = @"config";
        /// <summary>Configuration file name</summary>
        private const string FILENAME = @"config.ncconf";
        /// <summary>Path of the configuration file.</summary>
        static private string s_configFileName = "";
        /// <summary>Path of the configuration folder.</summary>
        static private string s_configDir = "";
        /// <summary> Default NCache tcp channel port. </summary>
        protected static int s_ncacheTcpPort = NCACHE_DEF_TCP_PORT;
        /// <summary> Default JvCache tcp channel port. </summary>
        protected static int s_jvcacheTcpPort = JVCACHE_DEF_TCP_PORT;
        /// <summary> Default http channel port. </summary>
        protected static int s_httpPort = DEF_HTTP_PORT;
        /// <summary> Default IPC channel port. </summary>
        protected static string s_ipcPortName = DEF_IPC_PORT_NAME;

        protected CacheConfigManager() { }
        /// <summary>
        /// static constructor
        /// </summary>
        static CacheConfigManager()
        {
            CacheConfigManager.ScanConfiguration();
        }

        /// <summary>
        /// Configuration files folder.
        /// </summary>
        static public string DirName
        {
            get { return s_configDir; }
        }

        /// <summary>
        /// Configuration file name.
        /// </summary>
        static public string FileName
        {
            get { return s_configFileName; }
        }

        /// <summary>
        /// Configuration file name.
        /// </summary>
        static public int NCacheTcpPort
        {
            get { return s_ncacheTcpPort; }
        }

        static public int JvCacheTcpPort
        {
            get { return s_jvcacheTcpPort; }
        }

        /// <summary>
        /// Configuration file name.
        /// </summary>
        static public int HttpPort
        {
            get { return s_httpPort; }
        }

        // <summary>
        /// Configuration file name.
        /// </summary>
        static public string IPCPortName
        {
            get { return s_ipcPortName; }
        }

        /// <summary>
        /// Scans the registry and locates the configuration file.
        /// </summary>
        static public void ScanConfiguration()
        {
            string REGKEY = @"Software\Alachisoft\NCache";
            try
            {
                AppUtil myUtil = new AppUtil();
                s_configDir = AppUtil.InstallDir;
                if (s_configDir == null || s_configDir.Length == 0)
                {
                    throw new ManagementException("Missing installation folder information: ROOTKEY= " + RegHelper.ROOT_KEY);
                }
                s_configDir = Path.Combine(s_configDir, DIRNAME);
                try
                {
                    if (!Directory.Exists(s_configDir))
                        Directory.CreateDirectory(s_configDir);
                }
                catch
                {

                }
                s_configFileName = Path.Combine(s_configDir, FILENAME);
                try
                {
                    if (!File.Exists(s_configFileName))
                    {
                        /// Save a dummy configuration.
                        SaveConfiguration(null);
                    }
                }
                catch
                {

                }

            }
            catch (ManagementException)
            {
                s_configFileName = "";
                throw;
            }
            catch (Exception e)
            {
                s_configFileName = "";
                throw new ManagementException(e.Message, e);
            }

            try
            {
                object v = RegHelper.GetRegValue(REGKEY, "NCacheTcp.Port",0);
                if (v != null)
                {
                    int port = Convert.ToInt32(v);
                    if (port >= System.Net.IPEndPoint.MinPort &&
                        port <= System.Net.IPEndPoint.MaxPort) s_ncacheTcpPort = port;
                }
            }
            catch (FormatException) { }
            catch (OverflowException) { }

            try
            {
                object v = RegHelper.GetRegValue(REGKEY, "TayzGridTcp.Port", 0);
                if (v != null)
                {
                    int port = Convert.ToInt32(v);
                    if (port >= System.Net.IPEndPoint.MinPort &&
                        port <= System.Net.IPEndPoint.MaxPort) s_jvcacheTcpPort = port;
                }
            }
            catch (FormatException) { }
            catch (OverflowException) { }

            try
            {
                object v = RegHelper.GetRegValue(REGKEY, "Http.Port",0);
                if (v != null)
                {
                    int port = Convert.ToInt32(v);
                    if (port >= System.Net.IPEndPoint.MinPort &&
                        port <= System.Net.IPEndPoint.MaxPort) s_httpPort = port;
                }
            }
            catch (FormatException) { }
            catch (OverflowException) { }
            try
            {
                object v = RegHelper.GetRegValue(REGKEY, "IPC.PortName",0);
                if (v != null)
                {
                    string portName = Convert.ToString(v);
                    if (portName != null)
                        s_ipcPortName = portName;
                }
            }
            catch (System.ArgumentException) { }
            catch (OverflowException) { }

        }

        /// <summary>
        /// Initialize a registered cache given by the ID.
        /// </summary>
        /// <param name="cacheId">A string identifier of configuration.</param>
        static public ArrayList GetCacheConfig(string cacheId, string userId, string password, bool inProc)
        {
            if (FileName.Length == 0)
            {
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt");
            }
            try
            {
                XmlConfigReader configReader = new XmlConfigReader(FileName, cacheId);
                ArrayList propsList = configReader.PropertiesList;
                ArrayList configsList = CacheConfig.GetConfigs(propsList);

                foreach (CacheConfig config in configsList)
                {
                    if (!inProc) inProc = config.UseInProc;
                    break;
                }

                if (inProc)
                {

                    bool isAuthorize = false;
                    Hashtable ht = (Hashtable)propsList[0];
                    Hashtable cache = (Hashtable)ht["cache"];
                    
                    return configsList;
                }
                return null;
            }

            catch (SecurityException)
            {
                throw;
            }

            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        /// <summary>
        /// Initialize a registered cache given by the ID.
        /// </summary>
        /// <param name="cacheId">A string identifier of configuration.</param>
        static public CacheConfig GetCacheConfig(string cacheId)
        {
            if (FileName.Length == 0)
            {
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt");
            }
            try
            {
                XmlConfigReader configReader = new XmlConfigReader(FileName, cacheId);
                return CacheConfig.FromProperties(configReader.Properties);
            }
            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        static public CacheServerConfig GetUpdatedCacheConfig(string cacheId, string partId, string newNode, ref ArrayList affectedNodes, bool isJoining)
        {
            if (FileName.Length == 0)
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt");
            
            try
            {
                XmlConfigReader configReader = new XmlConfigReader(FileName, cacheId);
                CacheServerConfig config = configReader.GetConfigDom();
                
                string list = config.Cluster.Channel.InitialHosts.ToLower();
                string[] nodes = list.Split(',');

                if (isJoining)
                {
                    foreach (string node in nodes)
                    {
                        string[] nodename = node.Split('[');
                        affectedNodes.Add(nodename[0]);
                    }

                    if (list.IndexOf(newNode) == -1)
                    {
                        list = list + "," + newNode + "[" + config.Cluster.Channel.TcpPort + "]";
                    }
                }
                else
                {
                    foreach (string node in nodes)
                    {
                        string[] nodename = node.Split('[');
                        if (nodename[0] != newNode)
                        {
                            affectedNodes.Add(nodename[0]);
                        }
                    }

                    list = string.Empty;
                    foreach (string node in affectedNodes)
                    {
                        if (list.Length == 0) 
                            list = node + "[" + config.Cluster.Channel.TcpPort + "]";
                        else
                            list = list + "," + node + "[" + config.Cluster.Channel.TcpPort + "]";
                    }
                }

                config.Cluster.Channel.InitialHosts = list;

                return config;
            }
            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        /// <summary>
        /// Loads and returns all cache configurations from the configuration file.
        /// </summary>
        static public CacheServerConfig[] GetConfiguredCaches()
        {
            if (FileName.Length == 0)
            {
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt.");
            }
            try
            {
                ConfigurationBuilder builder = new ConfigurationBuilder(FileName);
                builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                builder.ReadConfiguration();
                Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCaches = new Alachisoft.NCache.Config.NewDom.CacheServerConfig[builder.Configuration.Length];
                builder.Configuration.CopyTo(newCaches, 0);

                return convertToOldDom(newCaches);
            }
            catch (ManagementException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

        private static Alachisoft.NCache.Config.Dom.CacheServerConfig[] convertToOldDom(Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCacheConfigsList)
        {
            IList<Alachisoft.NCache.Config.Dom.CacheServerConfig> oldCacheConfigsList = new List<Alachisoft.NCache.Config.Dom.CacheServerConfig>();
            for (int index = 0; index < newCacheConfigsList.Length; index++)
            {
                try
                {
                    oldCacheConfigsList.Add(Alachisoft.NCache.Config.NewDom.DomHelper.convertToOldDom(newCacheConfigsList[index]));
                }
                catch (Exception)
                {

                }
            }
            Alachisoft.NCache.Config.Dom.CacheServerConfig[] oldCacheConfigsArray = new CacheServerConfig[oldCacheConfigsList.Count];
            oldCacheConfigsList.CopyTo(oldCacheConfigsArray,0);
            return oldCacheConfigsArray;
        }
        /// <summary>
        /// Save caches to configuration
        /// </summary>
        static public void SaveConfiguration(Hashtable caches, Hashtable partitionedCaches)
        {
            if (FileName.Length == 0)
            {
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt.");
            }

            List<CacheServerConfig> configurations = new List<CacheServerConfig>();
            if (caches != null)
            {
                IDictionaryEnumerator ide = caches.GetEnumerator();
                while (ide.MoveNext())
                {
                    try
                    {
                        CacheInfo cacheInfo = (CacheInfo)ide.Value;
                        configurations.Add(cacheInfo.CacheProps);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
          
            SaveConfiguration(convertToNewDom(configurations).ToArray());
        }

        private static List<Alachisoft.NCache.Config.NewDom.CacheServerConfig> convertToNewDom(List<CacheServerConfig> oldCacheConfigsList)
        {
            List<Alachisoft.NCache.Config.NewDom.CacheServerConfig> newCacheConfigsList = new List<Alachisoft.NCache.Config.NewDom.CacheServerConfig>();

            IEnumerator itr = oldCacheConfigsList.GetEnumerator();
            while (itr.MoveNext())
            {
                Alachisoft.NCache.Config.Dom.CacheServerConfig tempOldCacheConfig = (Alachisoft.NCache.Config.Dom.CacheServerConfig) itr.Current;
                try
                {
                    Alachisoft.NCache.Config.NewDom.CacheServerConfig tempNewCacheConfig = Alachisoft.NCache.Config.NewDom.DomHelper.convertToNewDom(tempOldCacheConfig);
                    newCacheConfigsList.Add(tempNewCacheConfig);
                }
                catch (Exception)
                {
                    
                }
            }
            return newCacheConfigsList;
        }

        /// <summary>
        /// Save the configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void SaveConfiguration(object[] configuration)
        {
            StringBuilder xml = new StringBuilder();
            xml.Append("<configuration>\r\n");
            if (configuration != null && configuration.Length > 0)
            {
                ConfigurationBuilder builder = new ConfigurationBuilder(configuration);
                builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                xml.Append(builder.GetXmlString());
            }
            xml.Append("\r\n</configuration>");
            WriteXmlToFile(xml.ToString());
        }

        /// <summary>
        /// Write the xml configuration string to c
        /// </summary>
        /// <param name="xml"></param>
        private static void WriteXmlToFile(string xml)
        {
            if (FileName.Length == 0)
            {
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt.");
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(FileName, FileMode.Create);
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

        public static CacheServerConfig[] GetConfiguredCaches(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                throw new ManagementException("Can not locate cache configuration file. Installation might be corrupt.");
            try
            {
                ConfigurationBuilder builder = new ConfigurationBuilder(filePath);
                builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                builder.ReadConfiguration();
                Alachisoft.NCache.Config.NewDom.CacheServerConfig[] newCaches = new Alachisoft.NCache.Config.NewDom.CacheServerConfig[builder.Configuration.Length];
                builder.Configuration.CopyTo(newCaches, 0);
                return convertToOldDom(newCaches);
            }
            catch (Exception e)
            {
                throw new ManagementException(e.Message, e);
            }
        }

    }
}