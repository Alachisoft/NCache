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

using System;
using System.Net;
using System.Timers;
using System.Threading;
using System.Diagnostics;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer
{
    public class ServiceHost
    {
        private const string TCP_CHANNEL_NAME = "NCache-stcp";
        private const string HTTP_CHANNEL_NAME = "NCache-shttp";
        private const string IPC_CHANNEL_NAME = "NCache-sipc";
        private const string EVAL_LASTREPORT_REGKEY = "LastReportTime";
        private const short MAX_EVALDAYS_REPORTING = 10;
        private object _stop_mutex = new object();
        private int _autoStartDelay = 0;
        private int _cacheStartDelay = 0;
        static string _cacheserver = "NCache";
        private ServerHost _nchost = new ServerHost();
        private SocketServer _socketServer;
        private Management.NCacheSniffer _ncacheSniffer;
        private System.Timers.Timer _evalWarningTask;
        private System.Timers.Timer _cacheServerEvalTask;
        private System.Timers.Timer _reactWarningTask;
        private SocketServer _managementSocketServer;

        public ServiceHost()
        {
            InitializeComponent();
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        public void InitializeComponent()
        {
            // 
            // ServiceHost
            // 

            try
            {
                if (ServiceConfiguration.AutoStartDelay != 0)
                    _autoStartDelay = ServiceConfiguration.AutoStartDelay;

                if (ServiceConfiguration.CacheStartDelay != 0)
                    _cacheStartDelay = ServiceConfiguration.CacheStartDelay;
            }
            catch (Exception)
            {
            }
            finally
            {
            }
        }

       

        

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void Start(string[] args)
        {
            try
            {
                int serverPort = SocketServer.DEFAULT_SOCK_SERVER_PORT;
                int sendBuffer = SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int receiveBuffer = SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int managementServerPort = SocketServer.DEFAULT_MANAGEMENT_PORT;

                // In case of compact serilization plus DS Provider in same dll we search in deploy dir brute forcely
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Common.Util.AssemblyResolveEventHandler.DeployAssemblyHandler);
                try
                {
                    serverPort = ServiceConfiguration.Port;
                    managementServerPort = ServiceConfiguration.ManagementPort;

                    if (ServiceConfiguration.SendBufferSize > 0)
                        sendBuffer = ServiceConfiguration.SendBufferSize;

                    if (ServiceConfiguration.ReceiveBufferSize > 0)
                        receiveBuffer = ServiceConfiguration.ReceiveBufferSize;
                }
                catch (Exception)
                {
                }

                _socketServer = new SocketServer(serverPort, sendBuffer, receiveBuffer);

                IPAddress clusterIp = null;
                IPAddress clientServerIp = null;

                clusterIp = ServiceConfiguration.BindToIP;

                clientServerIp = ServiceConfiguration.BindToIP;
                
                _managementSocketServer = new SocketServer(managementServerPort, sendBuffer, receiveBuffer);

                _managementSocketServer.Start(clusterIp, Alachisoft.NCache.Common.Logger.LoggerNames.CacheManagementSocketServer, "NManagement Service", CommandManagerType.NCacheManagement, ConnectionManagerType.Management);
                _socketServer.Start(clientServerIp, Alachisoft.NCache.Common.Logger.LoggerNames.SocketServerLogs, "NCache Service", CommandManagerType.NCacheService, ConnectionManagerType.ServiceClient);

                CacheServer.SocketServerPort = managementServerPort;
                _nchost.CacheServer.SynchronizeClientConfig();
                _nchost.CacheServer.ClusterIP = clusterIp.ToString();
                _nchost.CacheServer.Renderer = _socketServer;
                _nchost.CacheServer.Renderer.ManagementIPAddress = clusterIp.ToString();
                _nchost.CacheServer.Renderer.ManagementPort = managementServerPort;
                Alachisoft.NCache.SocketServer.CacheProvider.Provider = _nchost.CacheServer;
    
                // Load configuration is separate thread to boost performance
                AssignMananagementPorts();
                ThreadPool.QueueUserWorkItem(new WaitCallback(GetRunningCaches));  
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                throw ex;
            }
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        public void Stop()
        {
            lock (_stop_mutex)
            {
                try
                {
                    Thread thread = new Thread(new ThreadStart(StopHosting));
                    thread.IsBackground = true;
                    thread.Start();
                    bool pulsed = Monitor.Wait(_stop_mutex, 18000); // we wait for 18 seconds. Default service timeout is 20000 (ms)
                    if (!pulsed)
                    {
                        AppUtil.LogEvent(_cacheserver, "Failed to stop caches on this server", EventLogEntryType.Warning, EventCategories.Error, EventID.GeneralInformation);
                    }
                    if (thread.IsAlive) thread.Abort();
                }
                catch (Exception)
                {
                }
            }
        }

        private void StopHosting()
        {
            try
            {
                if (_socketServer != null)
                    _socketServer.StopListening();

                if (_managementSocketServer != null)
                    _managementSocketServer.StopListening();

                if (_nchost != null)
                {
                    _nchost.StopHosting();
                    _nchost.Dispose();
                }

                if (_socketServer != null)
                    _socketServer.Stop();

                if (_managementSocketServer != null)
                    _managementSocketServer.Stop();

                if (_evalWarningTask != null)
                {
                    _evalWarningTask.Close();
                }

                if (_cacheServerEvalTask != null)
                {
                    _cacheServerEvalTask.Close();
                }

            }
            catch (ThreadAbortException te)
            {
                return;
            }
            catch (ThreadInterruptedException te)
            {
                return;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                try
                {
                    lock (_stop_mutex)
                    {
                        Monitor.PulseAll(_stop_mutex);
                    }
                }
                catch (Exception) { }
            }
        }
        
        private void GetRunningCaches(object info)
        {
            try
            {
                //AutoStartCaches(null);
            }
            catch (Exception ex)
            {
            }
        }

        private void AssignMananagementPorts()
        {
            try
            {
                _nchost.CacheServer.LoadConfiguration();
                _nchost.CacheServer.AssignManagementPorts();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private void AssignServerstoRunningCaches()
        {
            try
            {
                OSInfo currentOS = OSInfo.Windows;
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    currentOS = OSInfo.Unix;
#endif
                if (currentOS == OSInfo.Windows)
                {
                    _nchost.CacheServer.AssignServerstoRunningCaches();
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, "An error occurred while assigning servers to running caches. " + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.ConfigurationError);
            }
        }
    }
}

