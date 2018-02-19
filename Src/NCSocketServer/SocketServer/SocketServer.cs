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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Web.Util;

using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.SocketServer
{
    /// <summary>
    /// An object of this class is called when Ncache service starts and stops
    /// </summary>
    public sealed class  SocketServer: CacheRenderer
    {
        int _serverPort;
        int _sendBuffer;
        int _recieveBuffer;
        public const int DEFAULT_SOCK_SERVER_PORT = 9800;
        public const int DEFAULT_MANAGEMENT_PORT = 8250;  
        public const int DEFAULT_SOCK_BUFFER_SIZE = 32768;

        //Size is specified in bytes (.net stream if grow more then 1.9GB will give memory exception that's why
        //we send multiple responses each containing objects of specified size.
        // 1 GB = 1 * 1024 * 1024 * 1024 = 1073741824
        // 1 GB = 1 * MB * KB * Bytes = 1073741824
        // If not specified in Service config then we will consider 1GB packeting
        public static long CHUNK_SIZE_FOR_OBJECT = 1073741824;    

        static Logs _logger = new Logs();        

        static bool _enableCacheServerCounters = true;
        PerfStatsCollector _perfStatsColl = null;
        static LoggingInfo _serverLoggingInfo = new LoggingInfo();

        /// <summary>
        /// The garbage collection timer class.
        /// </summary>
        GarbageCollectionTimer gcTimer;

        ConnectionManager _conManager;

        LoggerNames _loggerName;


        private IConnectionManager _hostClientConnectionManager;
        decimal _clusterHealthDetectionInterval = 3;
        int _managementServerPort;
        string _managementServerIP;

        private string cacheName;

        public string CacheName
        {
            set { cacheName = value; }
        }

        public  IConnectionManager HostClientConnectionManager
        {
            get { return _hostClientConnectionManager; }
        }


        /// <summary>
        /// Initializes the socket server with the given port.
        /// </summary>
        /// <param name="port"></param>
        /// <param name="sendBufferSize"></param>
        /// <param name="recieveBufferSize"></param>
        public SocketServer(int port, int sendBufferSize, int recieveBufferSize)
        {
            _serverPort = port;
            _sendBuffer = sendBufferSize;
            _recieveBuffer = recieveBufferSize;
        }
        
        /// <summary>
        /// Gets the socket server port.
        /// </summary>
        public int ServerPort
        {
            get { return _serverPort; }
            set { _serverPort = value < 0 ? DEFAULT_SOCK_SERVER_PORT : value; }
        }

        /// <summary>
        /// Gets the send buffer size of connected client socket.
        /// </summary>
        public int SendBufferSize
        {
            get { return _sendBuffer; }
            set { _sendBuffer = value < 0 ? DEFAULT_SOCK_BUFFER_SIZE : value; }
        }

        public static Logs Logger
        {
            get { return _logger; }
        }
        /// <summary>
        /// Gets the receive buffer size of connected client socket.
        /// </summary>
        public int ReceiveBufferSize
        {
            get { return _recieveBuffer; }
            set { _recieveBuffer = value < 0 ? DEFAULT_SOCK_BUFFER_SIZE : value; }
        }

        /// <summary>
        /// Gets a value indicating whether Cache Server counters are enabled or not.
        /// </summary>
        public static bool IsServerCounterEnabled
        {
            get { return _enableCacheServerCounters; }
            set { _enableCacheServerCounters = value; }
        }

        public override int ManagementPort
        {
            get { return _managementServerPort; }
            set { _managementServerPort = value; }
        }

        public override string ManagementIPAddress
        {
            get { return _managementServerIP; }
            set { _managementServerIP = value; }
        }

   
        /// <summary>
        /// Starts the socket server.It registers some types with compact Framework, 
        /// enables simple logs as well as DetailedLogs, then it checks Ncache licence information.
        /// starts connection manager and perfmon counters.
        /// </summary>
        /// <param name="bindIP" ></param>
        /// 
        public void Start(IPAddress bindIP, LoggerNames loggerName, string perfStatColInstanceName, CommandManagerType cmdMgrType, ConnectionManagerType conMgrType)
        {
            if (loggerName == null)
                _loggerName = LoggerNames.SocketServerLogs;
            else
                _loggerName = loggerName;
            InitializeLogging();
        
            _perfStatsColl = new PerfStatsCollector("NCache Server", _serverPort);
            _conManager = new ConnectionManager(_perfStatsColl);
            
            _conManager.Start(bindIP, _serverPort, _sendBuffer, _recieveBuffer, _logger, cmdMgrType, conMgrType);

            if (ConnectionManagerType.HostClient == conMgrType)
            {
                _hostClientConnectionManager = _conManager;
            }
            

            //We initialize PerfstatsCollector only for SocketServer's instance for client.
            //Management socket server has just DUMMY stats collector.
            if (conMgrType == ConnectionManagerType.HostClient)
                _perfStatsColl.InitializePerfCounters();
        }

        /// <summary>
        /// Initialize logging by reading setting from application configuration file
        /// </summary>
        public void InitializeLogging()
        {
            bool enable_logs = false;
            bool detailed_logs = false;

            try
            {

                enable_logs = ServiceConfiguration.EnableLogs;
                detailed_logs = ServiceConfiguration.EnableDetailedLogs;

                this.InitializeLogging(enable_logs, detailed_logs);
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Initialize logging
        /// </summary>
        /// <param name="enable">Enable error logging only</param>
        /// <param name="detailed">Enable detailed logging</param>
        public void InitializeLogging(bool errorOnly, bool detailed)
        {
            try
            {
                if (errorOnly || detailed)
                {
                    Logs localLogger=new Logs();
                    localLogger.NCacheLog = new NCacheLogger();
                    localLogger.NCacheLog.Initialize(LoggerNames.SocketServerLogs);

                    if (detailed)
                    {
                        localLogger.NCacheLog.SetLevel("all");
                        localLogger.IsErrorLogsEnabled = true;
                    }
                    else
                    {
                        localLogger.NCacheLog.SetLevel("info");
                    }
                    localLogger.NCacheLog.Info("SocketServer.Start", "server started successfully");

                    ///Set logging status
                    if (errorOnly)
                        _serverLoggingInfo.SetStatus(LoggingInfo.LoggingType.Error, LoggingInfo.LogsStatus.Enable);
                    if (detailed)
                        _serverLoggingInfo.SetStatus(LoggingInfo.LoggingType.Detailed,
                            (detailed ? LoggingInfo.LogsStatus.Enable : LoggingInfo.LogsStatus.Disable));
                    localLogger.IsDetailedLogsEnabled = detailed;
                    localLogger.IsErrorLogsEnabled = errorOnly;

                    _logger= localLogger;
                   
                }
                else
                {
                    if (_logger.NCacheLog != null)
                    {
                        _logger.NCacheLog.Flush();
                        _logger.NCacheLog.SetLevel("OFF");
                    }
                }
            }
            catch (Exception) { throw; }
        }

        /// <summary>
        /// Stops the socket server. Stops connection manager.
        /// </summary>
        public void Stop()
        {
            if (_conManager != null)
            {
                _conManager.Stop();
            }
        }

        public void StopListening()
        {
            if (_conManager != null)
                _conManager.StopListening();
        }
        ~SocketServer()
        {
            if (_conManager != null)
            {
                _conManager.Stop();
                _conManager = null;
            }

            if (_perfStatsColl != null)
            {
                _perfStatsColl.Dispose();
                _perfStatsColl = null;
            }

        }

        /// <summary>
        /// Gets Server Port.
        /// </summary>
        public override int Port
        {
            get
            {
                return ServerPort;
            }
        }

        /// <summary>
        /// Converts server IpAddress string to IPAddress instance and return that.
        /// </summary>
        public override IPAddress IPAddress
        {
            get
            {
                try
                {
                    return IPAddress.Parse(ConnectionManager.ServerIpAddress);
                }
                catch (Exception ex)
                {
                    return null;
                }
            }
        }

        public override decimal ClusterHealthDetectionInterval
        {
            get
            {
                lock (this)
                {
                    return this._clusterHealthDetectionInterval;
                }
            }
        }

        /// <summary>
        /// Get current logging status for specified type
        /// </summary>
        /// <param name="subsystem"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override LoggingInfo.LogsStatus GetLoggingStatus(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type)
        {
            switch (subsystem)
            {
                case LoggingInfo.LoggingSubsystem.Server:
                    lock (_serverLoggingInfo)
                    {
                        return _serverLoggingInfo.GetStatus(type);
                    }
                case LoggingInfo.LoggingSubsystem.Client:
                    return ConnectionManager.GetClientLoggingInfo(type);
                default:
                    return LoggingInfo.LogsStatus.Disable;
            }
        }

        /// <summary>
        /// Set and apply logging status
        /// </summary>
        /// <param name="subsystem"></param>
        /// <param name="type"></param>
        /// <param name="status"></param>
        public override void SetLoggingStatus(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type, LoggingInfo.LogsStatus status)
        {
            if (subsystem == LoggingInfo.LoggingSubsystem.Client)
            {
                bool updateClient = false;
                switch (type)
                {
                    case LoggingInfo.LoggingType.Error:
                    case LoggingInfo.LoggingType.Detailed:
                        updateClient = ConnectionManager.SetClientLoggingInfo(type, status);
                        break;
                    case (LoggingInfo.LoggingType.Error | LoggingInfo.LoggingType.Detailed):
                        bool updateErrorLogs = ConnectionManager.SetClientLoggingInfo(LoggingInfo.LoggingType.Error, status);
                        bool updateDetailedLogs = ConnectionManager.SetClientLoggingInfo(LoggingInfo.LoggingType.Detailed, status);
                        updateClient = (updateErrorLogs || updateDetailedLogs);
                        break;
                }

                if (updateClient)
                    ConnectionManager.UpdateClients();
            }
            else if (subsystem == LoggingInfo.LoggingSubsystem.Server)
            {
                switch (status)
                {
                    case LoggingInfo.LogsStatus.Disable:

                       
                        ///If error logs are disabled, then disable both
                        if (type == LoggingInfo.LoggingType.Error ||
                            type == (LoggingInfo.LoggingType.Error | LoggingInfo.LoggingType.Detailed))
                        {
                            this.InitializeLogging(false, false);
                        }
                        else if (type == LoggingInfo.LoggingType.Detailed)
                        {
                            this.InitializeLogging(Logger.IsErrorLogsEnabled, false);
                        }

                        break;

                    case LoggingInfo.LogsStatus.Enable:

                        bool error = Logger.IsErrorLogsEnabled;
                        bool detailed = Logger.IsDetailedLogsEnabled;

                        if (type == LoggingInfo.LoggingType.Error)
                        {
                            error = true;
                            detailed = false;
                        }
                        else if (type == LoggingInfo.LoggingType.Detailed |
                            type == (LoggingInfo.LoggingType.Error | LoggingInfo.LoggingType.Detailed))
                        {
                            error = true;
                            detailed = true;
                        }

                        this.InitializeLogging(error, detailed);

                        break;
                }
            }
        }
        

        /// <summary>
        /// Start the gc timer to collect GEN#2 after specified intervals.
        /// </summary>
        /// <param name="dueTime">Time to wait (in minutes) before first collection.</param>
        /// <param name="period">Time between two consective GEN#2 collections.</param>
        public void StartGCTimer(int dueTime, int period)
        {
            try
            {
            }
            catch (Exception e)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error( "SocketServer.StartGCTimer", e.ToString());
            }
        }
	}
}
