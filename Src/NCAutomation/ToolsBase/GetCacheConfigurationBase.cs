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
                        if (!string.IsNullOrEmpty(Name)) _filename = Name + ".ncconf";
                        else _filename = "caches-config.ncconf";
                        Path =  Path + System.IO.Path.DirectorySeparatorChar + _filename;
                    }
                }
                else
                {
                    Path = System.IO.Path.Combine(AppUtil.InstallDir, @"bin"+ System.IO.Path.DirectorySeparatorChar + "tools");
                    if (!string.IsNullOrEmpty(Name)) _filename = Name + ".ncconf";
                    else _filename = "caches-config.ncconf";
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
                    StringBuilder xml = new StringBuilder();
                    List<Alachisoft.NCache.Config.NewDom.CacheServerConfig> configurations = new List<Alachisoft.NCache.Config.NewDom.CacheServerConfig>();

                    if (!string.IsNullOrEmpty(Name))
                    {
                        Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(Name);

                        if (serverConfig == null)
                            throw new Exception("Specified cache is not registered on the given server.");
                        serverConfig.CacheDeployment = null;
                        OutputProvider.WriteLine("Creating configuration for cache '{0}' registered on server '{1}:{2}'.", Name, NCache.ServerName, NCache.Port);
                        configurations.Add(serverConfig);
                        ConfigurationBuilder builder = new ConfigurationBuilder(configurations.ToArray());
                        builder.RegisterRootConfigurationObject(typeof(Alachisoft.NCache.Config.NewDom.CacheServerConfig));
                        xml.Append(builder.GetXmlString());
                        WriteXmlToFile(xml.ToString());
                        OutputProvider.WriteLine("Cache configuration saved successfully at " + Path + ".");
                    }
                    else
                    {
                        Alachisoft.NCache.Config.NewDom.CacheServerConfig[] serverConfigs = cacheServer.GetCacheServerConfiguration();
                        if (serverConfigs == null)
                            throw new Exception("No configured cache(s) found on the server.");
                        OutputProvider.WriteLine("Creating configuration for caches registered on server '{0}:{1}'.", NCache.ServerName, NCache.Port);
                        foreach (Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig in serverConfigs)
                        {
                            configurations.Add(serverConfig);
                        }
                        SaveConfiguration(configurations.ToArray());
                        OutputProvider.WriteLine("Configuration saved successfully at " + Path + ".");
                    }
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

        public void SaveConfiguration(object[] configuration)
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
