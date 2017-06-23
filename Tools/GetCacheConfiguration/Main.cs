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


namespace Alachisoft.NCache.Tools.GetCacheConfiguration
{

    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {   
                GetCacheConfigurationTool.Run(args);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
    /// <summary>
    /// Summary description for ConfigureCacheTool.
    /// </summary>
    /// 
    public class GetCacheConfigurationParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        string _path = string.Empty;
        string _file = string.Empty;
        private string _cacheId = string.Empty;
        private string _server = string.Empty;
        private int _port = -1;

       public GetCacheConfigurationParam()
        {
        }

        [ArgumentAttribute(@"/T", @"/path")]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        //[ArgumentAttribute(@"/f", @"/file")]
        public string FileName
        {
            get { return _file; }
            set { _file = value; }
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
    sealed class GetCacheConfigurationTool : CommandLineParamsBase
    {

        static private GetCacheConfigurationParam ccParam = new GetCacheConfigurationParam();
        static private NCacheRPCService NCache = new NCacheRPCService("");
        /// <summary>
        /// Sets the application level parameters to those specified at the command line.
        /// </summary>
        /// <param name="args">array of command line parameters</param>

        static public bool IsValidIP(string ip)
        {
            IPAddress adress;
            return IPAddress.TryParse(ip, out adress);
        }
        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        private static bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(ccParam.CacheId))
            {
                Console.Error.WriteLine("Error: CacheId not specified.");
                return false;
            }

            if (string.IsNullOrEmpty(ccParam.Server))
            {
                Console.Error.WriteLine("Error: Server not specified.");
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
                ncLog.Source = "NCache:GetCacheConfiguration Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        /// <summary>
        /// The main entry point for the tool.
        /// </summary>
        static public void Run(string[] args)
        {
            string failedNodes = string.Empty;
            ICacheServer cacheServer = null;

            try
            {
                object param = new GetCacheConfigurationParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                ccParam = (GetCacheConfigurationParam)param;

                if (ccParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(ccParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                string _filename = null;
                string _path = null;
                if (ccParam.Path != null && ccParam.Path != string.Empty)
                {
                    if (!Path.HasExtension(ccParam.Path))
                    {
                       
                        _filename = ccParam.CacheId + ".ncconf";
                        ccParam.Path = ccParam.Path + "\\" + _filename;
                    }
                }
                else
                {
                    ccParam.Path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    _filename = ccParam.CacheId + ".ncconf";
                    ccParam.Path = ccParam.Path + "\\" + _filename;
                }

                if (ccParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (!string.IsNullOrEmpty(ccParam.Server))
                {
                    NCache.ServerName = ccParam.Server;
                }
                else
                    NCache.ServerName = System.Environment.MachineName;

                if (ccParam.Port != -1)
                {
                    NCache.Port = ccParam.Port;
                }

                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                if (cacheServer != null)
                {
                    Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(ccParam.CacheId);
                    
                    if (serverConfig == null) throw new Exception("Specified cache is not registered on given server.");
                    
                    serverConfig.CacheDeployment = null;
                    
                    Console.WriteLine("Creating configuration for cache '{0}' registered on server '{1}:{2}'.", ccParam.CacheId, NCache.ServerName, NCache.Port);
                    
                    StringBuilder xml = new StringBuilder();
                    List<Alachisoft.NCache.Config.NewDom.CacheServerConfig> configurations = new List<Alachisoft.NCache.Config.NewDom.CacheServerConfig>();
                    configurations.Add(serverConfig);
                    ConfigurationBuilder builder = new ConfigurationBuilder(configurations.ToArray());
                    builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                    xml.Append(builder.GetXmlString());
                    WriteXmlToFile(xml.ToString());
                   
                    Console.WriteLine("Cache configuration saved successfully at " + ccParam.Path + ".");
                }

            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error : {0}", e.Message);
            }
            finally
            {
                NCache.Dispose();
                if ( cacheServer != null )
                    cacheServer.Dispose();
            }
        }

        private static void WriteXmlToFile(string xml)
        {
            if (ccParam.Path.Length == 0)
            {
                throw new ManagementException("Can not locate path for writing config.");
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(ccParam.Path, FileMode.Create);
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

