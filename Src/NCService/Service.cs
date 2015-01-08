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
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Configuration;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System.Net;

using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Http;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.SocketServer;
using System.Runtime.InteropServices;
using System.Threading;
using Alachisoft.NCache.Common;
using System.Timers;
using Alachisoft.NCache.Config.Dom;
//using Alachisoft.NCache.CloudLicensing.UsageLogging.Tasks;
using Alachisoft.NCache.Common.Threading;
//using Alachisoft.NCache.CloudLicensing.UsageLogging.LogReports.Dom;
using System.Collections.Generic;
//using Alachisoft.NCache.CloudLicensing.UsageLogging.Delegates;

namespace Alachisoft.NCache.Service
{

    class Service : System.ServiceProcess.ServiceBase
    {
        private const string TCP_CHANNEL_NAME = "NCache-stcp";
        private const string HTTP_CHANNEL_NAME = "NCache-shttp";
        private const string IPC_CHANNEL_NAME = "NCache-sipc";
        private const string EVAL_LASTREPORT_REGKEY = "LastReportTime";
        private const short MAX_EVALDAYS_REPORTING = 10;
        private string _clusterIp = string.Empty;
        private object _stop_mutex = new object();
        private int _cacheStartDelay = 0;

        static string _cacheserver = "NCache";

        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components;

        private CacheHost _nchost;
        private SocketServer.SocketServer _socketServer;
        private Management.NCacheSniffer _ncacheSniffer;
        private System.Timers.Timer _evalWarningTask;
        private SocketServer.SocketServer _managementSocketServer;
        //ReportingTasksHandler _reportingTaskHandler;

        internal CacheServer CacheServer
        {
            get { return this._nchost.CacheServer; }
        }

        public Service()
        {
            // This call is required by the Windows.Forms Component Designer.
            InitializeComponent();
        }

        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            AppUtil.LogEvent(_cacheserver, ((Exception)e.ExceptionObject).ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.UnhandledException);            
        }

        // The main entry point for the process
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            System.ServiceProcess.ServiceBase[] ServicesToRun;

            // More than one user Service may run within the same process. To add
            // another service to this process, change the following line to
            // create a second service object. For example,
            
            ServicesToRun = new System.ServiceProcess.ServiceBase[] { new Service() };

            System.ServiceProcess.ServiceBase.Run(ServicesToRun);
        }

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            // 
            // ServiceHost
            // 

            this.ServiceName = "NCacheSvc";

           
            try
            {

                string ip = ConfigurationSettings.AppSettings["NCacheServer.BindToIP"];

                if (ip.Length > 0)
                {
                    _clusterIp = Convert.ToString(ip);
                }
                string cacheStartDelay = ConfigurationSettings.AppSettings["NCacheServer.CacheStartDelay"];
                if (cacheStartDelay != null && cacheStartDelay.Length > 0)
                {
                    _cacheStartDelay = Convert.ToInt32(cacheStartDelay);
                }

            }
            catch (Exception)
            {
            }
            finally
            {
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    if (components != null)
                    {
                        components.Dispose();
                    }
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            try
            {
                int serverPort = SocketServer.SocketServer.DEFAULT_SOCK_SERVER_PORT;
                int sendBuffer = SocketServer.SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int receiveBuffer = SocketServer.SocketServer.DEFAULT_SOCK_BUFFER_SIZE;
                int managementServerPort = SocketServer.SocketServer.DEFAULT_MANAGEMENT_PORT;

                //Numan Hanif: in case of compact serilization plus DS Provider in same dll we search in deploy dir brute forcely
                AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Common.Util.AssemblyResolveEventHandler.DeployAssemblyHandler);
                try
                {

                    string configPort = ConfigurationSettings.AppSettings["NCacheServer.Port"];

                    if (configPort.Length > 0)
                    {
                        serverPort = Convert.ToInt32(configPort);
                    }

                    string managePort = ConfigurationSettings.AppSettings["NCacheServer.ManagementPort"];

                    if (managePort.Length > 0)
                    {
                        managementServerPort = Convert.ToInt32(managePort);
                    }

                    string configSendBuffer = ConfigurationSettings.AppSettings["NCacheServer.SendBufferSize"];

                    if (configSendBuffer.Length > 0)
                    {
                        sendBuffer = Convert.ToInt32(configSendBuffer);
                    }

                    string configReceiveBuffer = ConfigurationSettings.AppSettings["NCacheServer.ReceiveBufferSize"];

                    if (configReceiveBuffer.Length > 0)
                    {
                        receiveBuffer = Convert.ToInt32(configReceiveBuffer);
                    }
                }
                catch (Exception)
                {
                }

                _nchost = new CacheHost();

                _socketServer = new SocketServer.SocketServer(serverPort, sendBuffer, receiveBuffer);

                try
                {
                    int period = -1;
                    string intervalString = ConfigurationSettings.AppSettings["GCCollectInterval"];
                    if (intervalString.Length > 0)
                    {
                        period = Convert.ToInt32(intervalString);
                    }
                    _socketServer.StartGCTimer(5, period);
                }
                catch (Exception)
                { }

                
                IPAddress clusterIp = null;
                if (_clusterIp != null && _clusterIp != string.Empty)
                {
                    try
                    {
                        clusterIp = IPAddress.Parse(_clusterIp);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid BindToIP address specified");
                    }
                }

                ValidateMapAddress("NCacheServer.MgmtEndPoint");
                ValidateMapAddress("NCacheServer.ServerEndPoint");
                
               

                _nchost.RegisterMonitorServer();
                //muds:
                //start the socket server separately.
                //Start the NCache Management socket server seperately
                _managementSocketServer = new SocketServer.SocketServer(managementServerPort, sendBuffer, receiveBuffer);

                _managementSocketServer.Start(clusterIp, Alachisoft.NCache.Common.Logger.LoggerNames.CacheManagementSocketServer, "NManagement Service", CommandManagerType.NCacheManagement);
                _socketServer.Start(clusterIp, Alachisoft.NCache.Common.Logger.LoggerNames.SocketServerLogs, "NCache Service", CommandManagerType.NCacheClient);

                CacheServer.SocketServerPort = managementServerPort;
                _nchost.CacheServer.SynchronizeClientConfig();
                _nchost.CacheServer.ClusterIP = _clusterIp;
                _nchost.CacheServer.Renderer = _socketServer;
                Alachisoft.NCache.SocketServer.CacheProvider.Provider = _nchost.CacheServer;
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
                throw ex;
            }
        }

        private void ValidateMapAddress(string configKey)
        {
            try
            {
                string mappingString = System.Configuration.ConfigurationSettings.AppSettings[configKey];
                if (!String.IsNullOrEmpty(mappingString))
                {
                    string[] mappingAddress = mappingString.Split(':');
                    if (mappingAddress.Length == 2)
                    {
                        try
                        {
                            IPAddress publicIP = IPAddress.Parse(mappingAddress[0]);
                        }
                        catch (Exception)
                        {
                            AppUtil.LogEvent("NCache", "Invalid IP address specified in " + configKey, EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                        }
                        try
                        {
                            int port = Convert.ToInt32(mappingAddress[1]);
                        }
                        catch (Exception)
                        {
                            AppUtil.LogEvent("NCache", "Invalid Port address specified in " + configKey, EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                        }
                    }
                    else
                    {
                        AppUtil.LogEvent("NCache", "Missing Port in " + configKey + ", Specify valid address as IPAddress:Port", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                    }
                }
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
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
                        AppUtil.LogEvent(_cacheserver, "Failed to stop caches on this server", EventLogEntryType.Warning, EventCategories.Error, EventID.GeneralError);
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

            } 
            catch (ThreadAbortException te)
            {
                return;
            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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

        public void OnLicensingFailure(Exception ex)
        {
            ServiceController sc = new ServiceController(this.ServiceName);
            if (sc.Status != ServiceControllerStatus.Stopped && sc.Status != ServiceControllerStatus.StopPending)
                sc.Stop();
            
        }
    }
}
