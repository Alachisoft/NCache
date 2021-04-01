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
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Management.Automation;
using System.Text;

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


        private bool IsUnderStateTransfer(string cacheId, CacheServerConfig settings)
        {
            bool isUnderStateTransfer = false;
            if (settings != null && settings.Cluster != null && settings.Cluster.Nodes != null)
            {
                foreach (var Node in settings.Cluster.Nodes)
                {
                    ICacheServer cacheserver = null;
                    try
                    {
                        //Try connecting to each node
                        NCacheRPCService nCacheRPCService = new NCacheRPCService(Node.Key.NodeName);
                        cacheserver = nCacheRPCService.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                        isUnderStateTransfer = cacheserver.IsUnderStateTransfer(cacheId);
                        if (isUnderStateTransfer)
                        {
                            return isUnderStateTransfer;
                        }
                    }
                    catch
                    {
                    } 
                }

            }
            return isUnderStateTransfer;

        }

        public void StopCache()
        {
            try
            {
                if (!ValidateParameters())
                    return;
                ICacheServer m = null;
                try
                {
                    m = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                }
                catch(Exception e)
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
                            config = m.GetCacheConfiguration(cache);
                            if (config != null && config.InProc)
                            {
                                throw new Exception("InProc caches cannot be stopped explicitly.");
                            }
                            getBindIp = m.GetClusterIP();
                            OutputProvider.WriteLine("Stopping cache '{0}' on server {1}:{2}.", cache, getBindIp, NCache.Port);
                                m.StopCache(cache, _partId);

                                m.StopCache(cache, string.Empty);
                            
                            OutputProvider.WriteLine("'{0}' successfully stopped on server {1}:{2}.\n", cache, getBindIp,
                                    NCache.Port);
                        }
                        catch (System.Security.SecurityException e)
                        {
                            OutputProvider.WriteErrorLine("Failed to stop '{0}'. Error: {1} ", cache, e.Message);
                      
                            OutputProvider.WriteErrorLine(e.ToString());
                            OutputProvider.WriteLine(Environment.NewLine);

                        }
                        catch (Exception e)
                        {
                            OutputProvider.WriteErrorLine("Failed to stop '{0}'.", cache);

                            OutputProvider.WriteErrorLine("Error: " + e.ToString());
                            OutputProvider.WriteLine(Environment.NewLine);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                OutputProvider.WriteErrorLine("Error : {0}", e.Message);
                


                OutputProvider.WriteErrorLine(e.ToString());
                OutputProvider.WriteLine(Environment.NewLine);
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
                currentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Automation.Util.AssemblyResolver.GetAssembly);
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



    }
}
