// Copyright (c) 2015 Alachisoft
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
using System.Diagnostics;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.Tools.Common;


namespace Alachisoft.NCache.Tools.RemoveCache
{
    class Application
    {
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                RemoveCacheTool.Run(args);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

        }
    }

    public class RemoveCacheParam :  Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _cacheId = string.Empty;  
        private int _port = -1;
        private string _server = string.Empty;

        public RemoveCacheParam()
        {
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

    sealed class RemoveCacheTool
    {
        /// <summary> NCache service controller. </summary>
        static private NCacheRPCService NCache = new NCacheRPCService("");

        static private ICacheServer cacheServer;
        static private string clusterId = string.Empty;
        static private RemoveCacheParam cParam = new RemoveCacheParam();

        /// <summary>
        /// Sets the application level parameters to those specified at the command line.
        /// </summary>
        /// <param name="args">array of command line parameters</param>
  

        /// <summary>
        /// Validate all parameters in property string.
        /// </summary>


        /// <summary>
        /// Log an event in event viewer.
        /// </summary>
        private static void LogEvent(string msg)
        {
            EventLogEntryType type = EventLogEntryType.Error;
            using (EventLog ncLog = new EventLog("Application"))
            {
                ncLog.Source = "NCache:RemoveLocalCache Cache Tool";
                ncLog.WriteEntry(msg, type);
            }
        }

        private static bool ValidateParameters()
        {
            // Validating CacheId
            if (string.IsNullOrEmpty(cParam.CacheId))
            {
                Console.Error.WriteLine("Error: Cache Name not specified");
                return false;
            }
            AssemblyUsage.PrintLogo(cParam.IsLogo);

            return true;
        }

        

        static public void Run(string[] args)
        {
            try
            {
                object param = new RemoveCacheParam();
                CommandLineArgumentParser.CommandLineParser(ref param, args);
                cParam = (RemoveCacheParam)param;
                if (cParam.IsUsage)
                {
                    AssemblyUsage.PrintLogo(cParam.IsLogo);
                    AssemblyUsage.PrintUsage();
                    return;
                }

                if (!ValidateParameters()) return;

                if (cParam.Port != -1)
                {
                    NCache.Port = cParam.Port;
                }
                
                if (cParam.Port == -1) NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
               
                if (cParam.Server != null || cParam.Server != string.Empty)
                {
                    NCache.ServerName = cParam.Server;
                }

            
                cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                Console.WriteLine("\nRemoving cache '{0}' on server {1}:{2}.", cParam.CacheId,
                                            cacheServer.GetBindIP(), NCache.Port);
                if (cacheServer != null)
                {
                    string getBindIp = string.Empty;
                    try
                    {
                        
                        Alachisoft.NCache.Config.NewDom.CacheServerConfig serverConfig = cacheServer.GetNewConfiguration(cParam.CacheId);

                        if (serverConfig == null)
                        {
                            throw new Exception("Specified cache does not exist.");
                        }
                        if (serverConfig.CacheSettings.CacheType == "clustered-cache")
                        {

                            foreach (Address node in serverConfig.CacheDeployment.Servers.GetAllConfiguredNodes())
                            {
                                try
                                {
                                    NCache.ServerName = node.IpAddress.ToString();
                                    cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));
                                    cacheServer.UnregisterCache(cParam.CacheId, null, false);
                                    Console.WriteLine("Cache '{0}' successfully removed from server {1}:{2}.\n",
                                        cParam.CacheId, NCache.ServerName, NCache.Port);
                                }
                                catch (Exception ex)
                                {
                                    Console.Error.WriteLine("Error: Failed to Rmove Cache '{0}' from server '{1}:{2}'. ",
                                        cParam.CacheId, NCache.ServerName, NCache.Port);
                                    Console.Error.WriteLine("Error Detail: '{0}'. ", ex.Message);

                                    LogEvent(ex.Message);
                                }
                                finally
                                {
                                    cacheServer.Dispose();
                                }
                            }
                        }
                        else
                        {
                            try
                            {
                                getBindIp = cacheServer.GetBindIP();
                                cacheServer.UnregisterCache(cParam.CacheId, null, false);
                                Console.WriteLine("Cache '{0}' successfully removed from server {1}:{2}.\n",
                                            cParam.CacheId, getBindIp, NCache.Port);
                            }
                            catch (Exception e)
                            {
                                throw e;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("Error: Failed to Rmove Cache '{0}' from server '{1}:{2}'. ",
                                   cParam.CacheId, cacheServer.GetBindIP(), NCache.Port);
                        Console.Error.WriteLine(ex.Message);
                        LogEvent(ex.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Error: {0}", e.Message);
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
            }
            finally
            {
                NCache.Dispose();
            }
        }
    }
}
