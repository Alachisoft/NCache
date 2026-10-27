//  Copyright (c) 2026 Alachisoft
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
using System.Net;
using System.Timers;
using System.Threading;
using System.Diagnostics;

using Alachisoft.NCache.Common;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Util;
using System.Runtime.InteropServices;
using Alachisoft.NCache.Licensing;
using static Alachisoft.NCache.Management.CacheServer;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Licensing.RegistryUtil;
using System.Globalization;
#if NETCORE
using Alachisoft.NCache.Licensing.RegistryUtil;
#endif
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
        private SocketServer _managementSocketServer;
#if NETCORE
        private Http.ServiceHttpServer _serviceHttpServer;
#endif
        private NCacheLogger _serviceLogger;

        private bool IsSnmpMonitoringEnabled
        {
            get
            {
                return ServiceConfiguration.EnableSnmpMonitoring;
            }
        }

        private bool IsEnablePrometheusMonitoring
        {
            get
            {
                return ServiceConfiguration.EnablePrometheusMonitoring;
            }
        }

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
        public void Start()
        {
            try
            {
                int serverPort = SocketServer.DEFAULT_SOCK_SERVER_PORT;
                int sendBuffer = SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int receiveBuffer = SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int managementServerPort = SocketServer.DEFAULT_MANAGEMENT_PORT;

                // Initialize service logger
                try
                {
                    if (_serviceLogger == null)
                    {
                        _serviceLogger = new NCacheLogger();
                        _serviceLogger.Initialize(LoggerNames.ServiceLogs, false, true);

                        // After initialization, set the ServiceLogger instance in NCacheServiceLogger
                        NCacheServiceLogger.Logger = _serviceLogger;
                    }
                }
                catch (Exception) { }

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

                _managementSocketServer.Start(clusterIp, Common.Logger.LoggerNames.SocketServerLogs, "NManagement Service", CommandManagerType.NCacheManagement, ConnectionManagerType.Management);
                _socketServer.Start(clientServerIp, Common.Logger.LoggerNames.SocketServerLogs, "NCache Service", CommandManagerType.NCacheService, ConnectionManagerType.ServiceClient);

               

                CacheServer.SocketServerPort = managementServerPort;
                _nchost.CacheServer.SynchronizeClientConfig();
                _nchost.CacheServer.ClusterIP = clusterIp.ToString();
                _nchost.CacheServer.Renderer = _socketServer;
                _nchost.CacheServer.Renderer.ManagementIPAddress = clusterIp.ToString();
                _nchost.CacheServer.Renderer.ManagementPort = managementServerPort;
                CacheProvider.Provider = _nchost.CacheServer;

#if NETCORE
                StartServiceHttpServer();
#endif

                // Load configuration is separate thread to boost performance
                AssignMananagementPorts();

                NCacheServiceLogger.LogInfo("NCache service has been started successfully.");

                ThreadPool.QueueUserWorkItem(new WaitCallback(GetRunningCaches));

                if (ServiceConfiguration.EnableAutoStartWebManagement)
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(StartWebManager));
                }

                AppUtil.LogEvent(_cacheserver, _cacheserver + " service has started successfully", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStart);
                // The metric server is the receiving end of NCacheServer.EnableMetricsPublishing:
                // cache hosts push their counters to this service, and the Prometheus agent inside
                // the metric server mirrors them into the prometheus-net registry that GET /metrics
                // on the service HTTP server renders. Without it the service answers every publish
                // with MetricServerNotInitialized.
                if (IsEnablePrometheusMonitoring)
                {
                    CacheServer.Instance.MetricServer = MetricServerFactory.GetMetricServer();
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                throw ex;

            }
        }

        private void GetRunningCaches(object state)
        {
            try
            {
                if (_autoStartDelay > 0)
                    Thread.Sleep(_autoStartDelay * 1000);
                CacheServerConfig[] configCaches = CacheConfigManager.GetConfiguredCaches();

                if (configCaches != null && configCaches.Length > 0)
                {
                    foreach (CacheServerConfig cacheServerConfig in configCaches)
                    {
                        if (cacheServerConfig.AutoStartCacheOnServiceStartup && !cacheServerConfig.IsRunning && cacheServerConfig.InProc == false)
                        {
                            try
                            {
                                _nchost.CacheServer.StartCache(cacheServerConfig.Name.Trim());

                                if (_cacheStartDelay > 0)
                                    Thread.Sleep(_cacheStartDelay * 1000);
                            }
                            catch (Exception ex)
                            {

                            }// all exceptions are logged in event logs. and are ignored here.                           
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                    bool pulsed = Monitor.Wait(_stop_mutex, 18000); //we wait for 18 seconds. Default service timeout is 20000 (ms)
                    if (!pulsed)
                    {
                        AppUtil.LogEvent(_cacheserver, "Failed to stop caches on this server", EventLogEntryType.Warning, EventCategories.Error, EventID.GeneralInformation);
                    }
                    if (thread.IsAlive)
                    {
#if !NETCORE
                        thread.Abort();
#elif NETCORE
                        thread.Interrupt();
#endif
                    }
                }
                catch (Exception)
                {

                }

                NCacheServiceLogger.LogInfo("NCache service has been stopped successfully.");

                try
                {
                    NCacheServiceLogger.Dispose();
                }
                catch (Exception) { }
            }
        }
#if NETCORE
        /// <summary>
        /// Starts the service HTTP server on NCacheServer.RestPort and registers its endpoints.
        /// New endpoints are added here: implement Http.IServiceHttpEndpoint and add a Register call.
        /// A failure here is logged but never prevents the service from starting.
        /// .NET only: the OSS .NET Framework build exists for the tools and never hosts the service.
        /// </summary>
        private void StartServiceHttpServer()
        {
            try
            {
                int httpPort = ServiceConfiguration.RestPort;
                if (httpPort <= 0)
                {
                    NCacheServiceLogger.LogInfo("Service HTTP server is disabled (NCacheServer.RestPort is not a positive port).");
                    return;
                }

                Http.ServiceHttpServer server = new Http.ServiceHttpServer(httpPort)
                    .Register(new Http.MetricsJsonEndpoint());

                // Prometheus scrapes GET /metrics on this same listener/port (no separate MetricServerPort
                // listener on .NET). The registry is process-wide, so registering here before the metric
                // server is created (see MetricServerFactory below) is safe.
                if (IsEnablePrometheusMonitoring)
                    server.Register(new Http.Endpoints.PrometheusMetricsEndpoint());

                server.Start();
                _serviceHttpServer = server;

                NCacheServiceLogger.LogInfo("Service HTTP server started on port " + httpPort + " with endpoints: " + server.DescribeEndpoints() + ".");
            }
            catch (Exception ex)
            {
                _serviceHttpServer = null;
                NCacheServiceLogger.LogError("Failed to start the service HTTP server. " + ex);
                AppUtil.LogEvent(_cacheserver, "Failed to start the service HTTP server. " + ex.Message, EventLogEntryType.Warning, EventCategories.Error, EventID.GeneralInformation);
            }
        }

        private void StopServiceHttpServer()
        {
            try
            {
                Http.ServiceHttpServer server = Interlocked.Exchange(ref _serviceHttpServer, null);
                if (server != null)
                    server.Stop();
            }
            catch (Exception ex)
            {
                NCacheServiceLogger.LogError("Failed to stop the service HTTP server. " + ex);
            }
        }
#endif

        private void StopHosting()
        {
            try
            {
                if (_socketServer != null)
                    _socketServer.StopListening();

                if (_managementSocketServer != null)
                    _managementSocketServer.StopListening();

#if NETCORE
                StopServiceHttpServer();
#endif

                if (_nchost != null)
                {
                    _nchost.StopHosting();
                    _nchost.Dispose();
                }

                if (_socketServer != null)
                    _socketServer.Stop();

                if (_managementSocketServer != null)
                    _managementSocketServer.Stop();

                AppUtil.LogEvent(_cacheserver, _cacheserver + " service has stopped successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.ServiceStop);
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

        private void StartWebManager(object info)
        {
            try
            {
                ExecutionStatus status = WebManagementProcessExecuter.AutoStartWebManager();

                switch (status)
                {
                    case ExecutionStatus.Started:
                        AppUtil.LogEvent("NCache Management Center has been started successfully at: http://" + ServiceConfiguration.BindToIP + ":9801", EventLogEntryType.Information);
                        break;
                    case ExecutionStatus.Error:
                        AppUtil.LogEvent("An error occurred while auto starting NCache Management Center.", EventLogEntryType.Error);
                        break;
                }
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, "An error occurred while auto starting NCache Management Center.\n" + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.ServiceStart);
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
                AppUtil.LogEvent(_cacheserver, "An error occurred while assigning management ports. " + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.ConfigurationError);

            }
        }

       

      



    }
}

