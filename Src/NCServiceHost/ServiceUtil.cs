using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
//using System.Diagnostics.Eventing.Reader;
using System.Security.AccessControl;
//using System.ServiceProcess;
using System.Configuration;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System.Net;

//using System.Runtime.Remoting;
//using System.Runtime.Remoting.Channels;
//using System.Runtime.Remoting.Channels.Http;
//using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.SocketServer;
using System.Runtime.InteropServices;
using System.Threading;
using Alachisoft.NCache.Common;
using System.Timers;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.ServiceHost
{
    public class ServiceUtil
    {
        private const string TCP_CHANNEL_NAME = "NCache-stcp";
        private const string HTTP_CHANNEL_NAME = "NCache-shttp";
        private const string IPC_CHANNEL_NAME = "NCache-sipc";
        private const string EVAL_LASTREPORT_REGKEY = "LastReportTime";
        private const short MAX_EVALDAYS_REPORTING = 10;
        private object _stop_mutex = new object();
        private int _autoStartDelay = 0;
        private int _cacheStartDelay = 0;
#if JAVA
        static string _cacheserver = "TayzGrid";
#else
        static string _cacheserver = "NCache";
#endif
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.Container components;

        private ServerHost _nchost = new ServerHost();
        //private CacheHost _ncCache = new CacheHost();
        private SocketServer.SocketServer _socketServer;
        private Management.NCacheSniffer _ncacheSniffer;
        private System.Timers.Timer _evalWarningTask;
        private System.Timers.Timer _cacheServerEvalTask;
        private System.Timers.Timer _reactWarningTask;
        private SocketServer.SocketServer _managementSocketServer;

        public ServiceUtil()
        {
            InitializeComponent();
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
#if JAVA
            this.ServiceName = "TayzGridSvc";
#else
           // this.ServiceName = "NCacheSvc";
#endif
            //			try
            //			{
            //				string configPort = ConfigurationSettings.AppSettings["Tcp.Port"];
            //				if(configPort.Length > 0)
            //				{
            //					tcpPort = Convert.ToUInt32(configPort);
            //				}
            //			}
            //			catch(Exception)
            //			{
            //			}
            //			finally
            //			{
            //				if(tcpPort < 1) tcpPort = CacheConfiguration.TCP_PORT;
            //			}
            //			try
            //			{
            //				string configPort = ConfigurationSettings.AppSettings["Http.Port"];
            //				if(configPort.Length > 0)
            //				{
            //					httpPort = Convert.ToUInt32(configPort);
            //				}
            //			}
            //			catch(Exception)
            //			{
            //			}
            //			finally
            //			{
            //				if(httpPort < 1) httpPort = CacheConfiguration.HTTP_PORT;
            //			}
            try
            {
#if JAVA
                string ip = ConfigurationSettings.AppSettings["CacheServer.BindToClusterIP"];
                if (ip.Length > 0)
                {
                    _clusterIp = Convert.ToString(ip);
                }
#endif

#if JAVA
                string ip = ConfigurationSettings.AppSettings["CacheServer.BindToClientServerIP"];
                if (ip.Length > 0)
                {
                    _clientServerIp = Convert.ToString(ip);
                }
#endif

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

        private void NotifyReactivateLicense(object source, ElapsedEventArgs e)
        {
            try
            {
                string message = "Reactivation is required. Machine requires " + Licensing.LicenseManager.NewLicenses + " licenses while it is activated only for " + Licensing.LicenseManager.PrevLicences + " license(s).";
            //    AppUtil.LogEvent(_cacheserver, message, EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
            }
            catch
            {

            }

        }

        private void NotifyEvalLicense(object source, ElapsedEventArgs e)
        {
            try
            {
                Licensing.LicenseManager.LicenseType mode = Licensing.LicenseManager.LicenseMode(null);
                if (mode == Licensing.LicenseManager.LicenseType.ActivePerNode || mode == Licensing.LicenseManager.LicenseType.ActivePerProcessor)
                {

                    if (_evalWarningTask != null)
                    {
                        _evalWarningTask.Dispose();
                        _evalWarningTask.Close();
                        _evalWarningTask = null;
                    }
                }
                else if (mode == Licensing.LicenseManager.LicenseType.InEvaluation)
                {
                    TimeSpan evalTime = System.DateTime.Now - Licensing.LicenseManager.EvaluationDt;
                    double daysRemaining = Licensing.LicenseManager.EvaluationPeriod - evalTime.Days;
                    bool writeEntry = (daysRemaining <= MAX_EVALDAYS_REPORTING);

                    if (writeEntry)
                    {
                        object value = RegHelper.GetRegValue(RegHelper.ROOT_KEY, EVAL_LASTREPORT_REGKEY, 0);
                        if (value != null)
                        {
                            DateTime reportingTime = DateTime.MinValue;
                            try
                            {
                                long ticks = Convert.ToInt64(value);
                                reportingTime = new DateTime(ticks);
                            }
                            catch (Exception)
                            {
                            }

                            DateTime today = DateTime.Now;
                            if (reportingTime.DayOfYear == today.DayOfYear && reportingTime.Year == today.Year)
                            {
                                writeEntry = false;
                            }
                        }
                    }

                    //EventLog eventLog = new EventLog("Application", ".");
                    //EventLogEntryCollection eventLogCollection = eventLog.Entries;
                    //int count = eventLogCollection.Count;
                    //for (int i = 0; i < count - 1; i++)
                    //{
                    //    EventLogEntry entry = eventLogCollection[i];
                    //    if (entry.Source == "NCache")
                    //    {
                    //        if (entry.TimeWritten.Date.Equals(DateTime.Now.Date) && entry.Message.StartsWith("NCache evaluation"))
                    //        {
                    //            writeEntry = false;
                    //            break;
                    //        }
                    //    }
                    //}

                    if (writeEntry)
                    {
                        DateTime dT = DateTime.Now.AddDays(daysRemaining);
                        if (daysRemaining <= MAX_EVALDAYS_REPORTING && daysRemaining > 1)
                        {
                            string msg = string.Format(_cacheserver + " evaluation of {0} days expires on {1}. Please purchase license keys or extend evaluation period by contacting support@alachisoft.com ", Licensing.LicenseManager.EvaluationPeriod, dT.Date);
                            EventLog.WriteEntry(_cacheserver, msg, EventLogEntryType.Warning);
                        }
                        else if (daysRemaining == 1 || daysRemaining == 0)
                        {
                            string msg = string.Format(_cacheserver + " evaluation of {0} days expires on {1}. It cannot be extended any more. Therefore, please purchase NCache license from sales@alachisoft.com and activate before expiration.", Licensing.LicenseManager.EvaluationPeriod, dT.Date);
                            EventLog.WriteEntry(_cacheserver, msg, EventLogEntryType.Warning);
                        }

                        RegHelper.SetRegValue(RegHelper.ROOT_KEY, EVAL_LASTREPORT_REGKEY, DateTime.Now.Ticks, 0);
                    }
                }

            }
            catch (Exception ex)
            {

            }
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        public void OnStart(string[] args)
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


                //Thread.Sleep(15000);
                _socketServer = new SocketServer.SocketServer(serverPort, sendBuffer, receiveBuffer);

                //#if VS2005
                //                _nchost.StartHosting(TCP_CHANNEL_NAME, HTTP_CHANNEL_NAME,IPC_CHANNEL_NAME);

                //#else
                IPAddress clusterIp = null;
                IPAddress clientServerIp = null;
#if JAVA
                if (_clusterIp != null && _clusterIp != string.Empty)
                {
                    try
                    {
                        clusterIp = IPAddress.Parse(_clusterIp);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid BindToClusterIP address specified");
                    }
                }
#else 
                clusterIp = ServiceConfiguration.BindToClusterIP;
#endif
#if JAVA
                if (_clientServerIp != null && _clientServerIp != string.Empty)
                {
                    try
                    {
                        clientServerIp = IPAddress.Parse(_clientServerIp);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Invalid BindToClientServerIP address specified");
                    }
                }
#else
                clientServerIp = ServiceConfiguration.BindToClientServerIP;
#endif 

                //if (clusterIp == null)
                //{

                //    _nchost.StartHosting(TCP_CHANNEL_NAME, HTTP_CHANNEL_NAME);
                //}
                //else
                //{
                //    _nchost.StartHosting(TCP_CHANNEL_NAME, HTTP_CHANNEL_NAME, _clusterIp);
                //}

                //_nchost.RegisterMonitorServer();
                ////muds:
                ////start the socket server separately.
                //_socketServer.Start(clientServerIp);
                //CacheServer.SocketServerPort = serverPort;
                //_nchost.CacheServer.SynchronizeClientConfig();

                //_nchost.CacheServer.Renderer = _socketServer;
                //Alachisoft.NCache.SocketServer.CacheProvider.Provider = _nchost.CacheServer;

                //_nchost.RegisterMonitorServer();
                //muds:
                //start the socket server separately.
                // _socketServer.Start(clientServerIp, Alachisoft.NCache.Common.Logger.LoggerNames.SocketServerLogs, "NCache Service", CommandManagerType.NCacheClient);
                //Start the NCache Management socket server seperately
                //File.WriteAllText(@"C:\service.txt", "Before M");               
                _managementSocketServer = new SocketServer.SocketServer(managementServerPort, sendBuffer, receiveBuffer);
#if JAVA
                _managementSocketServer.Start(clusterIp, Alachisoft.NCache.Common.Logger.LoggerNames.CacheManagementSocketServer, "TGManagement Service", CommandManagerType.NCacheManagement);
                _socketServer.Start(clientServerIp, Alachisoft.NCache.Common.Logger.LoggerNames.SocketServerLogs, "TayzGrid Service", CommandManagerType.NCacheClient);
#else
                _managementSocketServer.Start(clusterIp, Alachisoft.NCache.Common.Logger.LoggerNames.CacheManagementSocketServer, "NManagement Service", CommandManagerType.NCacheManagement, ConnectionManagerType.Management);
                _socketServer.Start(clientServerIp, Alachisoft.NCache.Common.Logger.LoggerNames.SocketServerLogs, "NCache Service", CommandManagerType.NCacheService, ConnectionManagerType.ServiceClient);
#endif
                CacheServer.SocketServerPort = managementServerPort;
                _nchost.CacheServer.SynchronizeClientConfig();
                _nchost.CacheServer.ClusterIP = clusterIp.ToString();
                _nchost.CacheServer.Renderer = _socketServer;
                _nchost.CacheServer.Renderer.ManagementIPAddress = clusterIp.ToString();
                _nchost.CacheServer.Renderer.ManagementPort = managementServerPort;
                Alachisoft.NCache.SocketServer.CacheProvider.Provider = _nchost.CacheServer;

                if (Licensing.LicenseManager.LicenseMode(null) == Licensing.LicenseManager.LicenseType.InEvaluation)
                {
                    _cacheServerEvalTask = new System.Timers.Timer();
                    _cacheServerEvalTask.Interval = 1000 * 60 * 60 * 12;// 12 hour interval.
                    _cacheServerEvalTask.Elapsed += new ElapsedEventHandler(CacheServer.NotifyEvalLicense);
                    _cacheServerEvalTask.Enabled = true;

                    ThreadPool.QueueUserWorkItem(new WaitCallback(CacheServer.NotifyEvalLicense));

                    _evalWarningTask = new System.Timers.Timer();
                    _evalWarningTask.Interval = 1000 * 60 * 60 * 12;// 12 hour interval.
                    _evalWarningTask.Elapsed += new ElapsedEventHandler(NotifyEvalLicense);
                    _evalWarningTask.Enabled = true;
                    NotifyEvalLicense(null, null);
                }
                // if reactivation exists than generate event after every day
                if (Licensing.LicenseManager.Reactivate)
                {
                    _reactWarningTask = new System.Timers.Timer();
                    _reactWarningTask.Interval = ServiceConfiguration.LicenseCheckInterval;//1 day
                    _reactWarningTask.Elapsed += new ElapsedEventHandler(NotifyReactivateLicense);
                    _reactWarningTask.Enabled = true;
                    NotifyReactivateLicense(null, null);
                }
                // Load configuration is separate thread to boost performance
                AssignMananagementPorts();
                AssignServerstoRunningCaches();
                ThreadPool.QueueUserWorkItem(new WaitCallback(GetRunningCaches));
                // ThreadPool.QueueUserWorkItem(new WaitCallback(AutoStartCaches));

                //AutoStartCaches(new object());
                //#endif
                //  
                StartPerfmonLogging();

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
        public void OnStop()
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
                    _nchost.CacheServer.DisposePerfmonSets();
                }
                if (_nchost != null)
                {
                    _nchost.CacheServer.DisposePerfmonSets();
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
              //  AppUtil.LogEvent(_cacheserver, ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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

        private void AutoStartCaches(object StateInfo)
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
                                _nchost.CacheServer.StartCache(cacheServerConfig.Name.Trim(), EncryptionUtil.Encrypt(ServiceConfiguration.CacheUserName), EncryptionUtil.Encrypt(ServiceConfiguration.CacheUserPassword != null ? Protector.DecryptString(ServiceConfiguration.CacheUserPassword) : null));
                                //CacheServerModerator.StartCache(cacheServerConfig.Name.Trim(), userId, password);
                               // AppUtil.LogEvent(_cacheserver, "The cache  '" + cacheServerConfig.Name.Trim() + "' started successfully.", EventLogEntryType.Information, EventCategories.Information, EventID.CacheStart);

                                if (_cacheStartDelay > 0)
                                    Thread.Sleep(_cacheStartDelay * 1000);
                            }
                            catch (Exception ex)
                            {

                                //   AppUtil.LogEvent(_cacheserver, "Exception Message  '" + ex, EventLogEntryType.Information, EventCategories.Information, EventID.CacheStart);
                            }// all exceptions are logged in event logs. and are ignored here.                           
                        }
                    }
                }
            }
            catch (Exception ex)
            {
              //  AppUtil.LogEvent(_cacheserver, "An error occurred while auto-starting caches. " + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.CacheStartError);
            }
        }

        private void GetRunningCaches(object info)
        {
            try
            {
                _nchost.CacheServer.LoadConfiguration();
                AutoStartCaches(null);
            }
            catch (Exception ex)
            {
              //  AppUtil.LogEvent(_cacheserver, "An error occurred while loading running caches. " + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.ConfigurationError);
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

        private void StartPerfmonLogging()
        {
            try
            {
                _nchost.CacheServer.StartPerfmonLogging();
            }
            catch (Exception ex)
            {

            }
        }

        private void AssignServerstoRunningCaches()
        {
            try
            {
                _nchost.CacheServer.AssignServerstoRunningCaches();

            }
            catch (Exception ex)
            {
                AppUtil.LogEvent(_cacheserver, "An error occurred while assigning servers to running caches. " + ex.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.ConfigurationError);
            }
        }
    }
}

