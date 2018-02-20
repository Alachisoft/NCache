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
// limitations under the License.


using Alachisoft.NCache.Automation.ToolsOutput;
using Alachisoft.NCache.Automation.ToolsParametersBase;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Reflection;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsLifecycle.Stop, "Cache")]
    public class StopCacheBase : StopCacheParameters, IConfiguration
    {
        /// <summary> NCache service controller. </summary>
        private NCacheRPCService NCache = new NCacheRPCService("");
        /// <summary> Cache ids specified at the command line. </summary>
        private ArrayList s_cacheId = new ArrayList();

        private string _partId = string.Empty;
        private string TOOLNAME = "StopCache Tool";

        /// <summary>
        /// Sets the application level parameters to those specified at the command line.
        /// </summary>
        /// <param name="args">array of command line parameters</param>
        public bool ValidateParameters()
        {
            if (Name != null && Name.Length > 0)
            {
                CachesList = new ArrayList();
                CachesList.AddRange(Name);
            }
            NCache = new NCacheRPCService("");
            NCache.ServerName = Server;
            if (String.IsNullOrEmpty(NCache.ServerName))
                NCache.ServerName = System.Environment.MachineName;
            NCache.Port = Port;
            if (NCache.Port == -1)
            {
                NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            }
            if (CachesList.Count == 0)
            {
                OutputProvider.WriteErrorLine("Error: cache name not specified.");
            }
            if (CachesList.Count > 1)
                _partId = string.Empty;
            ToolsUtil.PrintLogo(OutputProvider, printLogo, TOOLNAME);
            return true;
        }

        public void StopCache()
        {

            try
            {
                if (!ValidateParameters())
                    return;
                ICacheServer m = null;
                OutputProvider.WriteLine("Licensed to use FREE of cost. Use As-is without support.\n");
                try
                {
                    m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch (Exception e)
                {
                    OutputProvider.WriteErrorLine("Error: NCache service could not be contacted on server.");
                }

                string getBindIp = string.Empty;
                CacheServerConfig config = null;
                if (m != null)
                {
                    foreach (string cache in CachesList)
                    {
                        try
                        {
                            config = m.GetCacheConfiguration((string)cache);
                            if (config != null && config.InProc)
                            {
                                throw new Exception("InProc caches cannot be stopped explicitly.");
                            }
                            getBindIp = m.GetBindIP();
                            OutputProvider.WriteLine("Stopping cache '{0}' on server {1}:{2}.", cache, getBindIp, NCache.Port);

                            m.StopCache(cache, _partId);

                            

                            OutputProvider.WriteLine("'{0}' successfully stopped on server {1}:{2}.\n", cache, getBindIp,
                                    NCache.Port);
                        }
                        catch (System.Security.SecurityException e)
                        {
                            OutputProvider.WriteErrorLine("Failed to stop '{0}'. Error: {1} ", cache, e.Message);

                            OutputProvider.WriteErrorLine(e.ToString());

                        }
                        catch (Exception e)
                        {
                            OutputProvider.WriteErrorLine("Failed to stop '{0}'.", cache);

                            OutputProvider.WriteErrorLine("Error: " + e.ToString());

                        }
                    }
                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);


                OutputProvider.WriteErrorLine(e.ToString());
            }
            finally
            {
                NCache.Dispose();
            }
        }

        public void InitializeCommandLinePrameters(string[] args)
        {
            object parameters = this;
            CommandLineArgumentParser.CommandLineParser(ref parameters, args);
        }

        protected override void BeginProcessing()
        {
            try
            {
#if NETCORE
                AppDomain currentDomain = AppDomain.CurrentDomain;
                currentDomain.AssemblyResolve += new ResolveEventHandler(GetAssembly);
#endif
                OutputProvider = new PowerShellOutputConsole(this);
                TOOLNAME = "Stop-Cache Cmdlet";
                StopCache();
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

#if NETCORE
        private static System.Reflection.Assembly GetAssembly(object sender, ResolveEventArgs args)
        {
            string final = "";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string bin = directoryInfo.Parent.Parent.FullName; //bin folder
                final = System.IO.Path.Combine(bin, "service"); /// from where you neeed the assemblies
            }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                string location = System.Reflection.Assembly.GetExecutingAssembly().Location;
                DirectoryInfo directoryInfo = Directory.GetParent(location); // current folder
                string installDir = directoryInfo.Parent.FullName; //linux install directory
                directoryInfo = Directory.GetParent(installDir); //go back one directory
                installDir = directoryInfo.FullName;
                final = Path.Combine(installDir, "lib");
            }
            return System.Reflection.Assembly.LoadFrom(Path.Combine(final, new AssemblyName(args.Name).Name + ".dll"));
        }
#endif

    }
}
