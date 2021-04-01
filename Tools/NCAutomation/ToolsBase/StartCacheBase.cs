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
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Management.Automation;

namespace Alachisoft.NCache.Automation.ToolsBase
{
    [Cmdlet(VerbsLifecycle.Start, "Cache")]
    public class StartCacheBase : StartCacheParameters, IConfiguration
    {
        static private string _partId = string.Empty;
        private string TOOLNAME = "StartCache Tool";
        NCacheRPCService NCache;
        void ApplyParameters()
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


        }

        void StartCacheOnServer()
        {
            string cacheIp = string.Empty;
            try
            {
                ICacheServer cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                CacheServerConfig config = null;
                if (cacheServer != null)
                {
                    cacheIp = cacheServer.GetClusterIP();
                    foreach (string cache in CachesList)
                    {
                        try
                        {
                            config = cacheServer.GetCacheConfiguration(cache);
                            
                            if (config != null && config.InProc)
                            {
                                throw new Exception("InProc caches cannot be started explicitly.");
                            }

                            OutputProvider.WriteLine("Starting cache '{0}' on server {1}:{2}.", cache, cacheIp, NCache.Port);

                            cacheServer.StartCache(cache, _partId);


                            OutputProvider.WriteLine("'{0}' successfully started on server {1}:{2}. \n", cache, cacheIp,
                             NCache.Port);
                        }
                        catch (SecurityException e)
                        {
                            OutputProvider.WriteErrorLine("Failed to start '{0}' on server {1}.", cache,
                                  cacheIp);
                            OutputProvider.WriteErrorLine(e.Message);                            

                        }
                        catch (Exception e)
                        {
                            OutputProvider.WriteErrorLine("Failed to start '{0}' on server {1}.", cache,
                                cacheIp);
                            OutputProvider.WriteErrorLine(e.ToString() + "\n");

                        }
                    }
                }
            }
            catch (ManagementException ex)
            {
                OutputProvider.WriteErrorLine("Error : {0}", "NCache service could not be contacted on server");

                OutputProvider.WriteErrorLine(ex.ToString());
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

        public void StartCacheProcess()
        {
            try
            {
                ApplyParameters();
                StartCacheOnServer();
            }
            catch
            {
                throw;
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
                TOOLNAME = "Start-Cache Cmdlet";
                StartCacheProcess();
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

        public bool ValidateParameters()
        {
            return false;
        }
        
    }
}