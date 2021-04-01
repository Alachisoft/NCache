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
using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Management.Automation;
using System.Net;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsData.Export, "CacheConfiguration") ]
    public class GetCacheConfigurationBase : GetCacheConfigurationParameters, IConfiguration
    {
        private NCacheRPCService NCache = new NCacheRPCService("");
        private const string TOOLNAME = "GetCacheConfiguration";

        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>
        public bool ValidateParameters()
        {
            if (string.IsNullOrEmpty(Name))
            {
               OutputProvider.WriteErrorLine("Error: CacheId not specified");
               return false;
            }


            if (string.IsNullOrEmpty(Server))
            {
                OutputProvider.WriteErrorLine("Error: Server not specified");
                return false;
            }
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);;
            return true;
        }

        public void GetCacheConfiguration()
        {
            string failedNodes = string.Empty;
            ICacheServer cacheServer = null;
            try
            {
                if (!ValidateParameters()) return;

                string _filename = null;
                string _path = null;
                if (Path != null && Path != string.Empty)
                {
                    if (!System.IO.Path.HasExtension(Path))
                    {
                        _filename = Name + ".ncconf";
                        Path =  Path + System.IO.Path.DirectorySeparatorChar + _filename;
                    }
                }
                else
                {
                    //Path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Path = System.IO.Path.Combine(AppUtil.InstallDir, @"bin"+ System.IO.Path.DirectorySeparatorChar + "tools");
                    _filename = Name + ".ncconf";
                    Path = Path + System.IO.Path.DirectorySeparatorChar + _filename;
                }

                if (Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
                if (!string.IsNullOrEmpty(Server))
                {
                    NCache.ServerName = Server;
                }
                else
                    NCache.ServerName = System.Environment.MachineName;

                if (Port != -1)
                {
                    NCache.Port = Port;
                }
                try
                {
                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                    return;
                }

                if (cacheServer != null)
                {
                    Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(Name);
                    if (serverConfig == null)
                        throw new Exception("Specified cache is not registered on the given server.");
                   serverConfig.CacheDeployment = null;
                   OutputProvider.WriteLine("Creating configuration for cache '{0}' registered on server '{1}:{2}'.", Name, NCache.ServerName, NCache.Port);
                   StringBuilder xml = new StringBuilder();
                   List<Alachisoft.NCache.Config.NewDom.CacheServerConfig> configurations = new List<Alachisoft.NCache.Config.NewDom.CacheServerConfig>();
                   configurations.Add(serverConfig);
                   ConfigurationBuilder builder = new ConfigurationBuilder(configurations.ToArray());
                   builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                   xml.Append(builder.GetXmlString());
                   WriteXmlToFile(xml.ToString());
                   OutputProvider.WriteLine("Cache configuration saved successfully at " + Path + ".");
                }

            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                
            }
            finally
            {

                NCache.Dispose();
                if (cacheServer != null)
                    cacheServer.Dispose();
            }
        }

        private void WriteXmlToFile(string xml)
        {
            if (Path.Length == 0)
            {
                throw new ManagementException("Can not locate path for writing config.");
            }

            FileStream fs = null;
            StreamWriter sw = null;

            try
            {
                fs = new FileStream(Path, FileMode.Create);
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

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                GetCacheConfiguration();
            }
            catch (Exception ex)
            {
                OutputProvider.WriteErrorLine(ex);
            }
        }

        protected override void ProcessRecord()
        {
            try { }
            catch { }

        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }
    }
}
