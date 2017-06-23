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
using Alachisoft.NCache.Management;
using System.Configuration;
using System.Net;
using Alachisoft.NCache.SocketServer;
using System.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Tools.Common;
using System.IO;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common;
using System.Diagnostics;
using Alachisoft.NCache.Common.Net;


namespace Alachisoft.NCache.CacheHost
{
    public class CacheSeparateHost 
    {
        private static Alachisoft.NCache.Management.CacheHost _nchost;
        private static SocketServer.SocketServer _socketServer;
        private static SocketServer.SocketServer _managementSocketServer;
        private static String cacheName;
        private static string _applicationName = "NCache";
        private static int managementPort = -1;
       
        private static int ErrorCode = 500;


        public static int Main(string[] args)
        {
            try
            {
                if (populateValues(args))
                {
                    StartCacheHost();
                    Console.WriteLine("Started");
                    return ErrorCode;
                }
                return ErrorCode;
            }
            catch (Exception ex)
            {
                close();
                AppUtil.LogEvent(_applicationName, "Cache [ " + cacheName + " ] Error:" + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                Console.Error.WriteLine(ex.ToString());
                return ErrorCode;
            }
        }

        private static void onCacheStopped()
        {
            Thread stopCacheThread = new Thread(new ThreadStart(close));
            stopCacheThread.Start();
        }

        private static bool populateValues(string[] args)
        {

            bool isValid = false;
            String errorMessage = "";
            try
            {
                if (args != null && args.Length > 0)
                {
                    object param = new CacheHostParam();
                    CommandLineArgumentParser.CommandLineParser(ref param, args);
                    CacheHostParam cParam = (CacheHostParam)param;
                    if (cParam.IsUsage)
                    {
                        AssemblyUsage.PrintLogo(cParam.IsLogo);
                        AssemblyUsage.PrintUsage();
                        return false;
                    }

                    if (!String.IsNullOrEmpty(cParam.CacheName))
                    {
                        cacheName = cParam.CacheName;
                        isValid = true;
                    }
                    else 
                    {
                        if (File.Exists(cParam.CacheConfigPath))
                        {
                            try
                            {
                                if (isCacheExist(cParam.CacheConfigPath))
                                {
                                    String cName = getCacheName(cParam.CacheConfigPath);
                                    if (!String.IsNullOrEmpty(cName))
                                    {
                                        cacheName = cName;
                                        isValid = true;
                                    }
                                    else
                                    {
                                        errorMessage = "Provided config.conf is not valid cache configurations.";
                                        isValid = false;
                                        ErrorCode = 1;
                                    }
                                }
                                else
                                {
                                    errorMessage = "Multiple cache configurations provided in config.conf.";
                                    isValid = false;
                                    ErrorCode = 2;
                                }
                            }
                            catch (Exception ex)
                            {
                                errorMessage = ex.Message;
                                isValid = false;
                            }
                        }
                        else
                        {
                            errorMessage = "Cache configuration [config.conf] path is not provided or File does not exist.";
                            isValid = false;
                            ErrorCode = 3;
                        }
                    }

                    throwError(errorMessage, isValid);

                    if (cParam.ManagementPort != -1 && cParam.ManagementPort != 0)
                    {
                        managementPort = cParam.ManagementPort;
                        isValid = true;
                    }
                    throwError(errorMessage, isValid);
                }
                else
                {
                    errorMessage = "Arguments not specified";
                    isValid = false;
                    throwError(errorMessage, isValid);
                }

            }
            catch (Exception ex)
            {
                throwError(ex.ToString(), false);
            }
            return true;
        }

        private static void StartCacheHost()
        {
            
            string _clientServerIp = string.Empty;
            IPAddress clusterIp = null;
            IPAddress clientServerIp = null;
            
            try
            {
               AssemblyResolveEventHandler.CacheName = cacheName;
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Alachisoft.NCache.Common.Util.AssemblyResolveEventHandler.DeployAssemblyHandler);
                int sendBuffer = SocketServer.SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int receiveBuffer = SocketServer.SocketServer.DEFAULT_SOCK_BUFFER_SIZE;


                if (ServiceConfiguration.SendBufferSize > 0)
                    sendBuffer = ServiceConfiguration.SendBufferSize;

                if (ServiceConfiguration.ReceiveBufferSize > 0)
                    receiveBuffer = ServiceConfiguration.ReceiveBufferSize;


                _nchost = new Management.CacheHost();
                _socketServer = new SocketServer.SocketServer(ServiceConfiguration.Port, sendBuffer, receiveBuffer);
                string part1 = Path.Combine(AppUtil.InstallDir, "bin");
                string part2 = Path.Combine(part1, "service");
                string serviceEXE = Path.Combine(part2, "Alachisoft.NCache.Service.exe");
                Configuration config = ConfigurationManager.OpenExeConfiguration(serviceEXE);

                try
                {
                    clusterIp = ServiceConfiguration.BindToIP;
                }
                catch (Exception)
                {
                    ErrorCode = 5;
                    throw new Exception("Invalid BindToClusterIP address specified.");
                }
                
                
                try
                {
                    clientServerIp = ServiceConfiguration.BindToIP;
                }
                catch (Exception)
                {
                    ErrorCode = 6;
                    throw new Exception("Invalid BindToClientServerIP address specified");
                }

                try
                {
                    if (!Helper.isPortFree(managementPort, clusterIp))
                    {
                        ErrorCode = 8;
                        throw new Exception("Management listener can not be started. The port might already be in use");
                    }

                }
                catch (Exception ex)
                {
                    if (ex.ToString().Contains("Specified argument was out of the range of valid values"))
                    {
                        ErrorCode = 10;
                        throw new Exception("The port is outside the range of valid values for Management.");
                    }
                    else
                        throw;
                }
                
                _socketServer.CacheName = cacheName;
               CacheProvider.Provider = _nchost.HostServer;
                _managementSocketServer = new SocketServer.SocketServer(managementPort, sendBuffer, receiveBuffer);


                _managementSocketServer.Start(clusterIp, Common.Logger.LoggerNames.CacheManagementSocketServer, "NManagement", CommandManagerType.NCacheHostManagement, ConnectionManagerType.Management);
                _socketServer.Start(clientServerIp, Common.Logger.LoggerNames.SocketServerLogs, "Cache Host", CommandManagerType.NCacheClient, ConnectionManagerType.HostClient);


                CacheServer.SocketServerPort = managementPort;
                CacheServer.ConnectionManager = _socketServer.HostClientConnectionManager;

                _nchost.HostServer.SynchronizeClientConfig();
                _nchost.HostServer.ClusterIP = clusterIp.ToString();
                _nchost.HostServer.Renderer = _socketServer;
                _nchost.HostServer.Renderer.ManagementIPAddress = clusterIp.ToString();
                OnCacheStopped cacheStoppedDelegate = new OnCacheStopped(onCacheStopped);
                _nchost.HostServer.RegisterCacheStopCallback(cacheStoppedDelegate);

                _nchost.HostServer.InitiateCacheHostStopThread();
                AppUtil.LogEvent(_applicationName, "Cache [ " + cacheName + " ] separate process is started successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.GeneralInformation);

            }
            catch (Exception e)
            {
                throwError(e.ToString(), false);
            }
        }


        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppUtil.LogEvent(_applicationName, "Cache [ " + cacheName + " ] Error:" + ((Exception)e.ExceptionObject).ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.UnhandledException);
        }

       
        private static bool isCacheExist(String fileName)
        {
            bool isCacheExist = false;
            try
            {
                CacheServerConfig[] configCaches = CacheConfigManager.GetConfiguredCaches(fileName);
                if (configCaches != null && configCaches.Length == 1)
                {
                    isCacheExist = true;
                }
            }
            catch (Exception ex)
            {
                throwError(ex.ToString(), false);
            }
            return isCacheExist;
        }

        private static String getCacheName(String fileName)
        {
            string name = "";
            try
            {
                CacheServerConfig[] configCaches = CacheConfigManager.GetConfiguredCaches(fileName);
                if (configCaches != null && configCaches.Length == 1)
                {
                    name = configCaches[0].Name;
                }
            }
            catch (Exception ex)
            {
                throwError(ex.ToString(), false);
            }
            return name;
        }

        private static void throwError(string errorMessage, bool isValid)
        {
            if (!isValid)
            {
                throw new Exception(errorMessage);
            }
        }

        private static void close()
        {
            try
            {
                if(_nchost != null)
                    if (_nchost.HostServer != null)
                    {
                        _nchost.HostServer.Dispose();
                    }

                if (_socketServer != null)
                {
                    _socketServer.Stop();
                }
                if (_managementSocketServer != null)
                {
                    _managementSocketServer.Stop();
                }
               
                AppUtil.LogEvent(_applicationName, "Cache [ " + cacheName + " ] separate process is stopped successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.GeneralInformation);
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_applicationName, "Cache [ " + cacheName + " ] Error: "+ ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
            }
            
        }
    }
}
