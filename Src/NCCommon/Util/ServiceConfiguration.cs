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
using System;
using System.Configuration;
using System.Net;
using System.IO;
using System.Diagnostics;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Common.Util
{
    public class ServiceConfiguration
    {
        private static int _port = 9800;
        private static long MB = 1024 * 1024;
        private static int _managementPort = 8250;
        private static int _sendBufferSize = 131072;
        private static bool _licenseLogging = false;
        private static int _receiveBufferSize = 131072;
        private static IPAddress _BindToIP;
        private static IPAddress _bindToClientServerIP;
        private static bool _enableDualSocket = false;
        private static bool _enableNagling = true;
        private static int _naglingSize = 500 * 1024; //500 kb
        private static string _cacheUserName;
        private static IPAddress _bindToIP;
        private static string _cacheUserPassword;
        private static bool _enableDebuggingCounters = false;
        private static bool _enableBadClientDetection = false;
        private static int _clientSocketSendTimeOut = 10;
        private static int _eventPriorityRatio = 30;
        private static int _eventBulkCount = 50;
        private static int _bulkEventCollectionInterval = 2;
        private static int _expirationBulkRemoveSize = 10;
        private static int _expirationBulkRemoveDelay = 0; //milliseconds
        private static int _evictionBulkRemoveSize = 10;
        private static int _evictionBulkRemoveDelay = 0; //milliseconds
        private static int _bulkItemsToReplicate = 50;
        private static bool _enableCacheLastAccessCount = false;
        private static bool _enableCacheLastAccessCountLogging = false;
        private static int _cacheLastAccessCountInterval = 10;
        private static DependencyLibrary _dependencyLibrary;
        private static int _cacheLastAccessLogInterval = 40;
        private static int _lOHPoolSize = 40;
        private static int _lOHPoolBufferSize = 512;
        private static int _cacheSizeThreshold = 80;
        private static int _cacheSizeReportInterval = 15; //minutes
        private static bool _logClientEvents = false;
        private static string _eventLogLevel;
        private static bool _enableForcedGC = true;
        private static int _forcedGCThreshold = 80; //percentage
        private static int _forcedGCInterval = 20; //seconds
        private static bool _enableLogs = false;
        private static bool _enableDetailedLogs = false;
        private static int _bufferSize = 1;
        private static bool _allowRequestEnquiry = false;
        private static int _requestEnquiryInterval = 10;
        private static int _requestEnquiryRetries = 1;
        private static string _nCacheSQLNotificationService;
        private static string _nCacheSQLNotificationQueue;
        private static int _responseDataSize = 30 * 1024; //1024 MB's
        private static int _enableSnapshotPoolingCacheSize = 100000; //number of item
        private static int _snapshotPoolSize = 10;
        private static int _snapshotCreationThreshold = 30; //seconds
        private static int _enumeratorChunkSize = 1000;
        private static int _requestInquiryCleanInterval = 10;
        private static bool _enableRequestInquiry = false;
        private static bool _eventsPersistence = false;
        private static int _eventsPersistenceInterval = 10;
        private static int _maxPendingConnections = 300;
        private static bool _enableServerCounters = true;
        private static int _maxResponseLength = 1;
        private static int _gracefullTimeout = 1;
        private static int _blockingActivity = 1;
        private static bool _disableIndexNotDefinedException = false;
        private static float _stateTransferDataSizePerSecond = 10;
        private static bool _enableGCDuringStateTransfer = false;
        private static long _gCThreshold = 1024 * MB * 2;
        private static bool _enableGCCollection = true;
        private static int _preparedQueryTableSize = 1000;
        private static int _preparedQueryEvictionPercentage = 10;
        private static long _licenseCheckInterval = 24 * 60 * 60 * 1000;
        private static bool _simulateSocketClose = false;
        private static int _socketCloseInterval = 60;
        private static bool _useAvgStats = false;
        private static bool _asyncTcpUpQueue = false;
        private static int _heartbeatInterval = 32000;
        private static bool _printClusterStats = false;
        private static TimeSpan _statsPrintInterval = new TimeSpan(10, 0, 0);
        private static bool _enableDebugLog = false;
        private static int _autoStartDelay = 0;
        private static int _cacheStartDelay = 0;
        private static string _unInstallInProgress = "false";
        private static string _mgmtEndPoint = "";
        private static string _serverEndPoint = "";
        private static int _gCCollectInterval = 0;
        private static decimal _clusterHealthDetectionInterval = 3;
        
        private static int _dataChunkSize = 512 * 1024;
        private static int _managementPortLower = 8300;
        private static int _managementPortUpper = 8400;
        private static string tempString;
        private static int tempInt;
        private static bool tempBool;
        private static long tempLong;
        private static bool turnOffRandomStartTime = false;
        private static decimal tempDecimal;
        private static TimeSpan tempTimeSpan;
        private static double _commandExecutionThreshold = 5; // seconds
        private static bool _enableCommandThresholdLogging = false;
        private static int _interval = 2 * 60;
        private static bool _enable = false;
        private static bool _archive = false;
        private static bool _perfmonHasChange = false;
        private static string _perfmonPath;

        private static int _touchInterval = 5; // seconds

        private static int _licenseInterval = 6;
        private static bool _enableLicenseInterval = false;
        private static bool _enableRequestCancellation = true;
        private static bool _publishCountersToCacheHost = false;

        private static int _acquireClientLicenseRetryInterval = 3;
        private static int _acquireClientLicenseRetries = 3;

        private static long _collectionChunkSize = 80000; // Bytes
        private static TimeSpan _pubSubClientInactivityTimeout = TimeSpan.FromSeconds(120);
        private static int _relicaJoinDelay = 0;//in seconds
        private static bool _enableObjectPooling = true;
        private static bool _enableAutoStartWebManagement = true;
        private static int _webManagerStartRetries = 5; //default 5 retries

        private static int _pauseWriterThreshold = 1024 * 1024 * 8; // default to 8 MB
        public static int _maxFeatureDataPostingDelay = 20 * 24 * 60;  //20 days

        public const string NETCORE_SERVICE_UNIX = "Alachisoft.NCache.Daemon.dll";
        public const string NETCORE_SERVICE_WIN = "Alachisoft.NCache.Service.dll";
        /// <summary>
        /// Touch interval in seconds
        /// </summary>
        public static int ItemTouchInterval
        {
            get { return _touchInterval; }
            set { _touchInterval = value; }
        }

        public static int RelicaJoinDelay
        {
            get { return _relicaJoinDelay; }
            set { _relicaJoinDelay = value; }
        }

        public static int AcquireClientLicenseRetries
        {
            get { return _acquireClientLicenseRetries; }
            set { _acquireClientLicenseRetries = value; }
        }
        public static int AcquireClientLicenseRetryInterval
        {
            get { return _acquireClientLicenseRetryInterval; }
            set { _acquireClientLicenseRetryInterval = value; }
        }
        public static IPAddress BindToIP
        {
            get { return _bindToIP; }
            private set { _bindToIP = value; }
        }

        public static int LicenseInterval
        {
            get { return _licenseInterval; }
            set { _licenseInterval = value; }
        }

        public static bool EnableCacheDebugging{ get; set;}

        public static bool EnableLicenseInterval
        {
            get { return _enableLicenseInterval; }
            set { _enableLicenseInterval = value; }
        }

        private static int loaderServicePort = 9850;
        private static int _serviceGCInterval = 180; //in minutes

        static bool isHotApply = false;


        private static int _notificationEventInterval = 1;
        private static string _nosdbDependencyProviderVersion = "1.0.0.0";
        private static int _nodeFailureRetryDelayInterval = 2000;//in msec


        private static int _threadsPerProcessor = 2;
        private static int _readerExpiration = -1;

        private static int _eventThreadPoolCount = 1;
        private static bool _useCommandThreadPool;

        private static Mechanism _communicationMechanism;
        private static bool _executeOperations;
        private static int _transactinalPoolCapacity = 3000;
        private static bool _enableUsageInfoLogs = true;
        private static bool _enableUsageInfoLogsForTesting = false;


        #region FeatureUsage
        private static int _featureDataCollectionInterval = 60;   //1 hour converted into milliseconds
        #endregion
        private static string _urlUsageInfo = "https://app.alachisoft.com/ncache";

        public static int EventThreadPoolCount 
        {
            get { return _eventThreadPoolCount; }
        }

        public static int ReaderExpiration
        {
            get { return _readerExpiration; }
            set { _readerExpiration = value; }
        }

        public static int ThreadsPerProcessor
        {
            get { return _threadsPerProcessor; }
        }

        public static Mechanism CommunicationMechanism
        {
            get { return _communicationMechanism; }
        }

        public static bool ExecuteOperations
        {
            get { return _executeOperations; }
        }

        public static bool UseCommandThreadPool
        {
            get { return _useCommandThreadPool; }
        }

        public static bool EnableUsageInfoLogs
        {
            get { return _enableUsageInfoLogs; }
            private set { _enableUsageInfoLogs = value; }
        }

        public static bool EnableUsageInfoLogsForTesting
        {
            get { return _enableUsageInfoLogsForTesting; }
            private set { _enableUsageInfoLogsForTesting = value; }
        }

        #region FeatureUsage
        public static int FeatureDataCollectionInterval { get => _featureDataCollectionInterval; set => _featureDataCollectionInterval = value; }
        public static int InitialLoggingDelayAtStartup { get; set; } = 20; //milliseconds

        public static int FeatureDataPostingDelay { get; set; } = 29 * 24 * 60;  // 30 days
        public static int FeatureDataPostingWaitTime { get; set; } = 24 * 60;  // 1 day

        public static int InitailFeatureDataPostingDelay { get; set; } = 3 * 24 * 60;  // 3 days

        public static bool EnableFeatureUsageLogging { get; set; } = true;

        public static bool UploadFeatureUsageStats { get; set; } = true;

        public static int UsageFailureRetriesCount { get; set; } = 3;
        #endregion

        static ServiceConfiguration()
        {
            Load();

        }

        public static bool LoadAutoRenewal()
        {
            System.Configuration.Configuration config;

            try
            {
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = "";
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_WIN);
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_UNIX);
#else
                serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");
#endif
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
            }
            catch (Exception ex) { return true; }

            bool autoRenewal = true;
            try
            {
                if (config.AppSettings.Settings["NCacheServer.AutoRenewal"] != null)
                {
                    autoRenewal = bool.Parse(config.AppSettings.Settings["NCacheServer.AutoRenewal"].Value);
                }
            }
            catch (Exception ex) { }

            return autoRenewal;

        }

        public static IPAddress LoadBindToIP()
        {
            System.Configuration.Configuration config = null;

            try
            {
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = "";
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_WIN);

                    if(!File.Exists(serviceEXE3))
                    {
                        var otherPath = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");

                        if (File.Exists(otherPath))
                        {
                            serviceEXE3 = otherPath;
                        }
                    }
                }

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                {
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_UNIX);
                }
#else
                serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");
#endif
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
            }
            catch
            {
            }

            var address = default(IPAddress);

            try
            {
                if (config != null && config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                {
                    address = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                }
            }
            catch
            {
            }

            return address;
        }

        public static void Load()
        {
            System.Configuration.Configuration config;

            try
            {
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = "";
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                {
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_WIN);
                    if(!File.Exists(serviceEXE3))
                    {
                        var otherPath = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");
                        if (File.Exists(otherPath))
                            serviceEXE3 = otherPath;
                    }
                }

                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                    serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_UNIX);
#else
                serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");
#endif
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
            }
            catch (Exception ex) { return; }

            if (!isHotApply)
                {

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ThreadsPerProcessor"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ThreadsPerProcessor"].Value);

                            if (tempInt > 0)
                                _threadsPerProcessor = tempInt;
                        }
                    }
                    catch (Exception ex) { }


                try
                {
                    if (config.AppSettings.Settings["NCacheServer.CommunicationMechanism"] != null)
                    {
                        switch (config.AppSettings.Settings["NCacheServer.CommunicationMechanism"].Value.ToLower())
                        {
                            case "pipelining":
                                _communicationMechanism = Mechanism.Pipelining;
                                break;

                            case "selector":
                                _communicationMechanism = Mechanism.Select;
                                break;

                            case "multibuffer":
                                _communicationMechanism = Mechanism.MultiBufferReceiving;
                                break;
                        }
                    }
                }
                catch { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ExecuteOperations"] != null)
                    {
                        switch (config.AppSettings.Settings["NCacheServer.ExecuteOperations"].Value.ToLower())
                        {
                            case "true":
                                _executeOperations = true;
                                break;

                            case "false":
                                _executeOperations = false;
                                break;
                        }
                    }
                }
                catch { }
                try
                {
                    if (config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                        BindToIP = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                }
                catch (Exception ex) { }
                try
                    {
                    if (config.AppSettings.Settings["NCacheServer.UseCommandThreadPool"] != null)
                        {
                        _useCommandThreadPool  = Boolean.Parse(config.AppSettings.Settings["NCacheServer.UseCommandThreadPool"].Value);
                        }
                    }
                    catch (Exception ex) { }

                try
                    {
                        if (config.AppSettings.Settings["NCacheServer.Port"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.Port"].Value);

                        if (tempInt > 0)
                                Port = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ManagementPort"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPort"].Value);
                        if (tempInt > 0)
                            ManagementPort = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.SendBufferSize"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SendBufferSize"].Value);
                            if (tempInt > 0)
                                SendBufferSize = tempInt;
                        }
                    }
                    catch (Exception ex) { }


                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ServiceGCCollectionInterval"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ServiceGCCollectionInterval"].Value);

                                ServiceGCInterval = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"].Value);
                        if (tempInt > 0)
                            ReceiveBufferSize = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                            BindToIP = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.BindToClientServerIP"] != null)
                            BindToClientServerIP = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.EnableDualSocket"] != null)
                            EnableDualSocket = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDualSocket"].Value);
                    }
                    catch (Exception ex) { }

                    if (config.AppSettings.Settings["NCacheServer.CacheUser"] != null)
                        CacheUserName = config.AppSettings.Settings["NCacheServer.CacheUser"].Value;

                    if (config.AppSettings.Settings["NCacheServer.CachePassword"] != null)
                        CacheUserPassword = config.AppSettings.Settings["NCacheServer.CachePassword"].Value;
                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationService"] != null)
                            NCacheSQLNotificationService = config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationService"].Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationQueue"] != null)
                            NCacheSQLNotificationQueue = config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationQueue"].Value;
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.Sections["DependencyLibrary"] != null)
                        {
                            DependencyLibrary = (DependencyLibrary)config.GetSection("DependencyLibrary");
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ManagementPortLower"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPortLower"].Value);
                            if (tempInt > 0)
                                ManagementPortLower = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.ManagementPortUpper"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPortUpper"].Value);
                            if (tempInt > 0)
                                ManagementPortUpper = tempInt;
                        }
                    }
                    catch (Exception ex) { }

                    isHotApply = true;

                    try
                    {
                        if (config.AppSettings.Settings["NCacheServer.EnableCacheDebugging"] != null)
                        {
                            EnableCacheDebugging = Boolean.Parse(config.AppSettings.Settings["NCacheServer.EnableCacheDebugging"].Value);
                        }
                    }
                    catch (Exception ex) { }

            try
                {
                    if (config.AppSettings.Settings["NCacheServer.LicenseLogging"] != null)
                        LicenseLogging = bool.Parse(config.AppSettings.Settings["NCacheServer.LicenseLogging"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.TouchInterval"] != null)
                        _touchInterval = Int32.Parse(config.AppSettings.Settings["NCacheServer.TouchInterval"].Value);
                    if (_touchInterval < 1)
                        _touchInterval = 5;
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableNagling"] != null)
                        EnableNagling = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableNagling"].Value);
                    }
                catch (Exception ex) { }

                //need to identify range for nagling size
                try
                {
                    if (config.AppSettings.Settings["NCache.NagglingSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCache.NagglingSize"].Value);
                        if (tempInt > 0)
                            NaglingSize = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"] != null)
                        EnableDebuggingCounters = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableBadClientDetection"] != null)
                        EnableBadClientDetection = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableBadClientDetection"].Value);
                }
                catch (Exception ex) { }

                //need to identify range for clientSocketSendTimeout 
                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ClientSocketSendTimeOut"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ClientSocketSendTimeOut"].Value);
                        if (tempInt > 5)
                            ClientSocketSendTimeout = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EventPriorityRatio"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventPriorityRatio"].Value);
                        if (tempInt > 0 &&  tempInt < 100)
                            EventPriorityRatio = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EventBulkCount"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventBulkCount"].Value);
                        if (tempInt > 0)
                            EventBulkCount = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.BulkEventCollectionInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BulkEventCollectionInterval"].Value);
                        if (tempInt > 0)
                            BulkEventCollectionInterval = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"] != null)
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"].Value);
                    if (tempInt > 0)
                        ExpirationBulkRemoveSize = tempInt;
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"].Value);
                        if (tempInt > 0)
                            ExpirationBulkRemoveDelay = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"].Value);
                        if (tempInt > 0)
                            EvictionBulkRemoveSize = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"].Value);
                        if (tempInt > 0)
                            EvictionBulkRemoveDelay = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.BulkItemsToReplicate"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BulkItemsToReplicate"].Value);
                        if (tempInt > 0)
                            BulkItemsToReplicate = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCount"] != null)
                        EnableCacheLastAccessCount = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCount"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCountLogging"] != null)
                        EnableCacheLastAccessCountLogging = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCountLogging"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.CacheLastAccessCountInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheLastAccessCountInterval"].Value);
                        if (tempInt > 0)
                            CacheLastAccessCountInterval = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.CacheLastAccessLogInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheLastAccessLogInterval"].Value);
                        if (tempInt > 0)
                            CacheLastAccessLogInterval = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {

                    if (config.AppSettings.Settings["NCacheServer.SendBufferSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SendBufferSize"].Value);
                        if (tempInt > 0)
                            SendBufferSize = tempInt;
                    }
                }
                catch (Exception ex) { }


                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ServiceGCCollectionInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ServiceGCCollectionInterval"].Value);

                        ServiceGCInterval = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {

                    if (config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"].Value);
                        if (tempInt > 0)
                            ReceiveBufferSize = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                        BindToIP = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.BindToIP"] != null)
                        BindToClientServerIP = IPAddress.Parse(config.AppSettings.Settings["NCacheServer.BindToIP"].Value);
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.EnableDualSocket"] != null)
                        EnableDualSocket = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDualSocket"].Value);
                }
                catch (Exception ex) { }

                if (config.AppSettings.Settings["NCacheServer.CacheUser"] != null)
                    CacheUserName = config.AppSettings.Settings["NCacheServer.CacheUser"].Value;

                if (config.AppSettings.Settings["NCacheServer.CachePassword"] != null)
                    CacheUserPassword = config.AppSettings.Settings["NCacheServer.CachePassword"].Value;
                try
                {
                    if (config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationService"] != null)
                        NCacheSQLNotificationService = config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationService"].Value;
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationQueue"] != null)
                        NCacheSQLNotificationQueue = config.AppSettings.Settings["NCacheServer.NCacheSQLNotificationQueue"].Value;
                }
                catch (Exception ex) { }

                try
                {
                    if (config.Sections["DependencyLibrary"] != null)
                    {
                        DependencyLibrary = (DependencyLibrary)config.GetSection("DependencyLibrary");
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ManagementPortLower"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPortLower"].Value);
                        if (tempInt > 0)
                            ManagementPortLower = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    if (config.AppSettings.Settings["NCacheServer.ManagementPortUpper"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPortUpper"].Value);
                        if (tempInt > 0)
                            ManagementPortUpper = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
                    var enableObjectPoolingRaw = config.AppSettings.Settings["NCacheServer.EnableObjectPooling"];

                    if (enableObjectPoolingRaw != null)
                    {
                        if (bool.TryParse(enableObjectPoolingRaw.Value, out var enableObjectPooling))
                        {
                            _enableObjectPooling = enableObjectPooling;
                        }
                    }
                }
                catch { }

                try
                {
                    var poolCapacitySettings = config.AppSettings.Settings["NCacheServer.TransactionalPoolCapacity"];

                    if (poolCapacitySettings != null)
                    {
                        if (int.TryParse(poolCapacitySettings.Value, out var poolCapacity))
                        {
                            if(poolCapacity>0)
                                _transactinalPoolCapacity = poolCapacity;
                        }
                    }
                }
                catch { }

                isHotApply = true;

            }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.LicenseLogging"] != null)
                    LicenseLogging = bool.Parse(config.AppSettings.Settings["NCacheServer.LicenseLogging"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.TouchInterval"] != null)
                    _touchInterval = Int32.Parse(config.AppSettings.Settings["NCacheServer.TouchInterval"].Value);
                if (_touchInterval < 1)
                    _touchInterval = 5;
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ReplicaJoinDelay"] != null)
                    _relicaJoinDelay = Int32.Parse(config.AppSettings.Settings["NCacheServer.ReplicaJoinDelay"].Value);
                if (_relicaJoinDelay < 1)
                    _relicaJoinDelay = 0;
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.EnableNagling"] != null)
                    EnableNagling = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableNagling"].Value);
            }
            catch (Exception ex) { }

            //need to identify range for nagling size
            try
            {

                if (config.AppSettings.Settings["NCache.NagglingSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCache.NagglingSize"].Value);
                    if (tempInt > 0)
                        NaglingSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"] != null)
                    EnableDebuggingCounters = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableBadClientDetection"] != null)
                    EnableBadClientDetection = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableBadClientDetection"].Value);
            }
            catch (Exception ex) { }

            //need to identify range for clientSocketSendTimeout 
            try
            {
                if (config.AppSettings.Settings["NCacheServer.ClientSocketSendTimeOut"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ClientSocketSendTimeOut"].Value);
                    if (tempInt > 5)
                        ClientSocketSendTimeout = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventPriorityRatio"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventPriorityRatio"].Value);
                    if (tempInt > 0 && tempInt < 100)
                        EventPriorityRatio = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventBulkCount"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventBulkCount"].Value);
                    if (tempInt > 0)
                        EventBulkCount = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.BulkEventCollectionInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BulkEventCollectionInterval"].Value);
                    if (tempInt > 0)
                        BulkEventCollectionInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"] != null)
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveSize"].Value);
                if (tempInt > 0)
                    ExpirationBulkRemoveSize = tempInt;
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ExpirationBulkRemoveDelay"].Value);
                    if (tempInt > 0)
                        ExpirationBulkRemoveDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveSize"].Value);
                    if (tempInt > 0)
                        EvictionBulkRemoveSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EvictionBulkRemoveDelay"].Value);
                    if (tempInt > 0)
                        EvictionBulkRemoveDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.BulkItemsToReplicate"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BulkItemsToReplicate"].Value);
                    if (tempInt > 0)
                        BulkItemsToReplicate = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCount"] != null)
                    EnableCacheLastAccessCount = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCount"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCountLogging"] != null)
                    EnableCacheLastAccessCountLogging = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCacheLastAccessCountLogging"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CacheLastAccessCountInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheLastAccessCountInterval"].Value);
                    if (tempInt > 0)
                        CacheLastAccessCountInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CacheLastAccessLogInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheLastAccessLogInterval"].Value);
                    if (tempInt > 0)
                        CacheLastAccessLogInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.LOHPoolSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.LOHPoolSize"].Value);
                    if (tempInt > 0)
                        LOHPoolSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.LOHPoolBufferSize"] != null)
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.LOHPoolBufferSize"].Value);
                if (tempInt > 0)
                    LOHPoolBufferSize = tempInt;
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CacheSizeThreshold"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheSizeThreshold"].Value);
                    if (tempInt > 0 && tempInt < 100)
                        CacheSizeThreshold = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CacheSizeReportInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheSizeReportInterval"].Value);
                    if (tempInt > 5)
                        CacheSizeReportInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.LogClientEvents"] != null)
                    LogClientEvents = bool.Parse(config.AppSettings.Settings["NCacheServer.LogClientEvents"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventLogLevel"] != null)
                {
                    tempString = config.AppSettings.Settings["NCacheServer.EventLogLevel"].Value;
                    if (!string.IsNullOrEmpty(tempString))
                        EventLogLevel = tempString;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.EnableForcedGC"] != null)
                    EnableForcedGC = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableForcedGC"].Value);
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.ForcedGCThreshold"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ForcedGCThreshold"].Value);
                    if (tempInt > 0 && tempInt < 100)
                        ForcedGCThreshold = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ForcedGCInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ForcedGCInterval"].Value);
                    if (tempInt > 5)
                        ForcedGCInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["EnableLogs"] != null)
                    EnableLogs = bool.Parse(config.AppSettings.Settings["EnableLogs"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["EnableDetailedLogs"] != null)
                    EnableDetailedLogs = bool.Parse(config.AppSettings.Settings["EnableDetailedLogs"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["BufferSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["BufferSize"].Value);
                    if (tempInt > 1)
                        BufferSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.AllowClusterRequestEnquiry"] != null)
                    AllowRequestEnquiry = bool.Parse(config.AppSettings.Settings["NCacheServer.AllowClusterRequestEnquiry"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ClusterRequestEnquiryInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ClusterRequestEnquiryInterval"].Value);
                    if (tempInt > 1)
                        RequestEnquiryInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            //need to idenfity upper limit of retries i.e request timeout = 60s, how many retries possible?
            try
            {
                if (config.AppSettings.Settings["NCacheServer.ClusterRequestEnquiryRetries"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ClusterRequestEnquiryRetries"].Value);
                    if (tempInt > 0)
                        RequestEnquiryRetries = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableSnapshotPoolingCacheSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EnableSnapshotPoolingCacheSize"].Value);
                    if (tempInt > 1)
                        EnableSnapshotPoolingCacheSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.SnapshotPoolSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SnapshotPoolSize"].Value);
                    if (tempInt > 1)
                        SnapshotPoolSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.SnapshotCreationThreshold"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SnapshotCreationThreshold"].Value);
                    if (tempInt > 1)
                        SnapshotCreationThreshold = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnumeratorChunkSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EnumeratorChunkSize"].Value);
                    if (tempInt > 1)
                        EnumeratorChunkSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.UninstallInProgress"] != null)
                {
                    UninstallInProgress = config.AppSettings.Settings["NCacheServer.UninstallInProgress"].Value;
                }
            }
            catch (Exception ex) { }




            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventsPersistence"] != null)
                    EventsPersistence = bool.Parse(config.AppSettings.Settings["NCacheServer.EventsPersistence"].Value);
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCache.MaxPendingConnections"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCache.MaxPendingConnections"].Value);
                    if (tempInt > 10)
                        MaxPendingConnections = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCache.EnableServerCounters"] != null)
                    EnableServerCounters = bool.Parse(config.AppSettings.Settings["NCache.EnableServerCounters"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventsPersistenceInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventsPersistenceInterval"].Value);
                    if (tempInt > 10)
                        EventsPersistenceInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.MaxResponseLength"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.MaxResponseLength"].Value);
                    if (tempInt > 1)
                        MaxResponseLength = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.BlockingActivityTimeout"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BlockingActivityTimeout"].Value);
                    if (tempInt > 1)
                        BlockingActivity = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.GracefullShutdownTimeout"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.GracefullShutdownTimeout"].Value);
                    if (tempInt > 1)
                        GracefullTimeout = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.DisableIndexNotDefinedException"] != null)
                    DisableIndexNotDefinedException = bool.Parse(config.AppSettings.Settings["NCacheServer.DisableIndexNotDefinedException"].Value);
            }
            catch (Exception ex) { }

           try
            {
                if (config.AppSettings.Settings["NCacheServer.StateTransferDataSizePerSecond"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.StateTransferDataSizePerSecond"].Value);
                    if (tempInt > 10)
                        StateTransferDataSizePerSecond = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableGCDuringStateTransfer"] != null)
                    EnableGCDuringStateTransfer = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableGCDuringStateTransfer"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.GCThreshold"] != null)
                {
                    tempLong = Int64.Parse(config.AppSettings.Settings["NCacheServer.GCThreshold"].Value);
                    if (tempLong > 10)
                        GCThreshold = tempLong * MB * 2;
                }
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCache.EnableGCCollection"] != null)
                    EnableGCCollection = bool.Parse(config.AppSettings.Settings["NCache.EnableGCCollection"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["preparedQueryTableSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["preparedQueryTableSize"].Value);
                    if (tempInt > 1)
                        PreparedQueryTableSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["preparedQueryEvictionPercentage"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["preparedQueryEvictionPercentage"].Value);
                    if (tempInt > 1 && tempInt < 100)
                        PreparedQueryEvictionPercentage = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.LicenseCheckInterval"] != null)
                {
                    tempLong = Int64.Parse(config.AppSettings.Settings["NCacheServer.LicenseCheckInterval"].Value);

                    if (tempLong >= 1)
                        _licenseCheckInterval = tempLong * 60 * 1000;

                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.TurnOffRandomStartTime"] != null)
                {
                    turnOffRandomStartTime = bool.Parse(config.AppSettings.Settings["NCacheServer.TurnOffRandomStartTime"].Value);
                }
            }
            catch (Exception ex) { }



            try
            {
                if (config.AppSettings.Settings["NCacheServer.LicenseInterval"] != null)
                {
                    EnableLicenseInterval = true;
                    LicenseInterval = Int32.Parse(config.AppSettings.Settings["NCacheServer.LicenseInterval"].Value);
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.AcquireClientLicenseRetries"] != null)
                {
                    AcquireClientLicenseRetries = Int32.Parse(config.AppSettings.Settings["NCacheServer.AcquireClientLicenseRetries"].Value);
                }
            }
            catch (Exception ex) { }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.AcquireClientLicenseRetryInterval"] != null)
                {
                    AcquireClientLicenseRetryInterval = Int32.Parse(config.AppSettings.Settings["NCacheServer.AcquireClientLicenseRetryInterval"].Value);
                }
            }
            catch (Exception ex) { }


            try
            {

                if (config.AppSettings.Settings["NCache.SimulateSocketClose"] != null)
                    SimulateSocketClose = bool.Parse(config.AppSettings.Settings["NCache.SimulateSocketClose"].Value);
            }
            catch (Exception ex) { }

            try
            {

                if (config.AppSettings.Settings["NCacheServer.SocketCloseInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SocketCloseInterval"].Value);
                    if (tempInt > 20)
                        SocketCloseInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["useAvgStats"] != null)
                    UseAvgStats = bool.Parse(config.AppSettings.Settings["useAvgStats"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["asyncTcpUpQueue"] != null)
                    AsyncTcpUpQueue = bool.Parse(config.AppSettings.Settings["asyncTcpUpQueue"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["printClusterStats"] != null)
                    PrintClusterStats = bool.Parse(config.AppSettings.Settings["printClusterStats"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["statsPrintInterval"] != null)
                {
                    tempTimeSpan = new TimeSpan(Int32.Parse(config.AppSettings.Settings["statsPrintInterval"].Value));
                    if (tempTimeSpan != null)
                        StatsPrintInterval = tempTimeSpan;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["EnableDebugLog"] != null)
                    EnableDebugLog = bool.Parse(config.AppSettings.Settings["EnableDebugLog"].Value);
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CacheStartDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.CacheStartDelay"].Value);
                    if (tempInt > 0)
                        CacheStartDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.AutoStartDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.AutoStartDelay"].Value);
                    if (tempInt > 0)
                        AutoStartDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.MgmtEndPoint"] != null)
                {
                    tempString = config.AppSettings.Settings["NCacheServer.MgmtEndPoint"].Value;
                    if (!String.IsNullOrEmpty(tempString))
                    {
                        string[] mappingAddress = tempString.Split(':');
                        if (mappingAddress.Length == 2)
                        {
                            try
                            {
                                IPAddress publicIP = IPAddress.Parse(mappingAddress[0]);
                            }
                            catch (Exception)
                            {
                                AppUtil.LogEvent("NCache", "Invalid IP address specified in NCacheServer.MgmtEndPoint", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                            }
                            try
                            {
                                int port = Convert.ToInt32(mappingAddress[1]);
                            }
                            catch (Exception)
                            {
                                AppUtil.LogEvent("NCache", "Invalid Port address specified in NCacheServer.MgmtEndPoint", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                            }
                        }
                        else
                        {
                            AppUtil.LogEvent("NCache", "Missing Port in NCacheServer.MgmtEndPoint, Specify valid address as IPAddress:Port", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                        }
                    }
                    ManagementEndPoint = tempString;
                }
            }
            catch (Exception ex) { }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"] != null)
                {
                    double tempdouble = double.Parse(config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"].Value);
                    if (tempdouble > 0)
                        CommandExecutionThreshold = tempdouble;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"] != null)
                {
                    bool tempbool = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"].Value);

                    EnableCommandThresholdLogging = tempbool;
                }
            }
            catch (Exception ex) { }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.PerfmonInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.PerfmonInterval"].Value);
                    if (tempInt > 5)
                        _interval = tempInt * 60;
                }
            }
            catch (Exception ex) { }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.PerfmonEnable"] != null)
                {
                    PerfmonEnable = bool.Parse(config.AppSettings.Settings["NCacheServer.PerfmonEnable"].Value);
                }
            }
            catch (Exception ex) { PerfmonEnable = false; }

            try
            {
                var enableAutoStartWebManagementRaw = config.AppSettings.Settings["NCacheServer.StartWebManagement"];

                if (enableAutoStartWebManagementRaw != null)
                {
                    if (bool.TryParse(enableAutoStartWebManagementRaw.Value, out var enableAutoStartWebManagement))
                    {
                        _enableAutoStartWebManagement = enableAutoStartWebManagement;
                    }
                }
                else
                {
                    _enableAutoStartWebManagement = true;
                }
            }
            catch (Exception ex) { }

            try
            {
                var webManagerStartRetriesRaw = config.AppSettings.Settings["NCacheServer.WebManagerStartRetries"];

                if (webManagerStartRetriesRaw != null)
                {
                    if (int.TryParse(webManagerStartRetriesRaw.Value, out var TempWebManagerStartRetries))
                    {
                        if (TempWebManagerStartRetries > 20)
                            _webManagerStartRetries = 20;
                        else if (TempWebManagerStartRetries < 2)
                            _webManagerStartRetries = 2;
                        else
                            _webManagerStartRetries = TempWebManagerStartRetries;
                    }
                }
                else
                {
                    _webManagerStartRetries = 5;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.PerfmonArchive"] != null)
                {
                    PerfmonArchive = bool.Parse(config.AppSettings.Settings["NCacheServer.PerfmonArchive"].Value);
                }
            }
            catch (Exception ex) { PerfmonArchive = false; }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.PerfmonLogPath"] != null)
                {
                    PerfmonPath = config.AppSettings.Settings["NCacheServer.PerfmonLogPath"].Value;
                }
                else
                    PerfmonPath = System.IO.Path.Combine(AppUtil.InstallDir, "log-files\\PerfmonLogsstatistics");
                if (String.IsNullOrEmpty(PerfmonPath))
                    PerfmonPath = System.IO.Path.Combine(AppUtil.InstallDir, "log-files\\PerfmonLogsstatistics");
            }
            catch (Exception ex)
            {
                PerfmonPath = System.IO.Path.Combine(AppUtil.InstallDir, "log-files\\PerfmonLogsstatistics");
            }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.ServerEndPoint"] != null)
                {
                    tempString = config.AppSettings.Settings["NCacheServer.ServerEndPoint"].Value;
                    if (!String.IsNullOrEmpty(tempString))
                    {
                        string[] mappingAddress = tempString.Split(':');
                        if (mappingAddress.Length == 2)
                        {
                            try
                            {
                                IPAddress publicIP = IPAddress.Parse(mappingAddress[0]);
                            }
                            catch (Exception)
                            {
                                AppUtil.LogEvent("NCache", "Invalid IP address specified in NCacheServer.ServerEndPoint", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                            }
                            try
                            {
                                int port = Convert.ToInt32(mappingAddress[1]);
                            }
                            catch (Exception)
                            {
                                AppUtil.LogEvent("NCache", "Invalid Port address specified in NCacheServer.ServerEndPoint", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                            }
                        }
                        else
                        {
                            AppUtil.LogEvent("NCache", "Missing Port in NCacheServer.ServerEndPoint, Specify valid address as IPAddress:Port", EventLogEntryType.Warning, EventCategories.Warning, EventID.GeneralInformation);
                        }
                    }
                    ServerEndPoint = tempString;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["GCCollectInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["GCCollectInterval"].Value);
                    if (tempInt > 0)
                        GCCollectInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.ClusterHealthDetectionInterval"] != null)
                {
                    tempDecimal = Decimal.Parse(config.AppSettings.Settings["NCacheServer.ClusterHealthDetectionInterval"].Value);
                    if (tempDecimal > 0)
                        ClusterHealthDetectionInterval = tempDecimal;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CancelTimedOutRequests"] != null)
                {
                    _enableRequestCancellation = bool.Parse(config.AppSettings.Settings["NCacheServer.CancelTimedOutRequests"].Value);

                }
            }
            catch (Exception ex) { }

         
            try
            {
                if (config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"] != null)
                {
                    double tempdouble = double.Parse(config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"].Value);
                    if (tempdouble > 0)
                        CommandExecutionThreshold = tempdouble;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"] != null)
                {
                    bool tempbool = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"].Value);

                    EnableCommandThresholdLogging = tempbool;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["DataChunkSize"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["DataChunkSize"].Value);
                    if (tempInt > 0)
                        DataChunkSize = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.NotificationEventInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.NotificationEventInterval"].Value);
                    if (tempInt > 1)
                        _notificationEventInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NosDBDependencyProviderVersion"] != null)
                {
                    string temp = config.AppSettings.Settings["NosDBDependencyProviderVersion"].Value;
                    if (!string.IsNullOrEmpty(temp))
                        NosDBDependencyProviderVersion = temp;
                }
            }
            catch (Exception ex)
            {
            }

            if (config.AppSettings.Settings["NCacheServer.ServerFailureRetryDelayInterval"] != null)
            {
                int configuredValue = _nodeFailureRetryDelayInterval;
                if (int.TryParse(config.AppSettings.Settings["NCacheServer.ServerFailureRetryDelayInterval"].Value, out configuredValue))
                {
                    if (configuredValue >= 0)
                    {
                        _nodeFailureRetryDelayInterval = configuredValue * 1000;//convert it into msec
                    }
                }
            }

            try
            {
                int readerExpire = -1;
                if (config.AppSettings.Settings["NCacheServer.ReaderExpiration"] != null)
                {
                    readerExpire = Int32.Parse(config.AppSettings.Settings["ReaderExpiration"].Value);
                    if (readerExpire > 0)
                        ReaderExpiration = readerExpire;
                }
            }
            catch (Exception ex) { }


            try
            {
                int eventThreadPool = -1;
                if (config.AppSettings.Settings["NCacheServer.EventThreadPoolCount"] != null)
                {
                    eventThreadPool = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventThreadPoolCount"].Value);
                    if (eventThreadPool > 0)
                        _eventThreadPoolCount = eventThreadPool;
                }
            }
            catch (Exception ex) { }

            try
            {
                long collectionChunkSize = -1;

                if (config.AppSettings.Settings["NCacheServer.CollectionChunkSize"] != null)
                {
                    collectionChunkSize = long.Parse(config.AppSettings.Settings["NCacheServer.CollectionChunkSize"].Value);

                    if (collectionChunkSize > 0)
                    {
                        CollectionChunkSize = collectionChunkSize;
                    }
                }
            }
            catch (Exception)
            {
            }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.PublishCountersToCacheHost"] != null)
                    PublishCountersToCacheHost = bool.Parse(config.AppSettings.Settings["NCacheServer.PublishCountersToCacheHost"].Value);
            }
            catch (Exception ex) { }


            //if (config.AppSettings.Settings["NCacheServer.StateTransferDataSizePerThread"] != null)
            //    StateTransferDataSizePerThread = config.AppSettings.Settings["NCacheServer.StateTransferDataSizePerThread"].Value;
            try
            {
                if (config.AppSettings.Settings["NCacheServer.PubSubClientInactivityTimeout"] != null)
                {
                    int seconds = Int32.Parse(config.AppSettings.Settings["NCacheServer.PubSubClientInactivityTimeout"].Value);

                    if ( seconds >= 30)
                        _pubSubClientInactivityTimeout = new TimeSpan(0, 0, seconds);

                    PubSubClientInactivityTimeout = _pubSubClientInactivityTimeout;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.PauseWriterThreshold"] != null)
                {
                    int threshold = Int32.Parse(config.AppSettings.Settings["NCacheServer.PauseWriterThreshold"].Value);
                    if (threshold < 64) threshold = 64;

                    _pauseWriterThreshold = threshold * 1024 ;
                }
            }
            catch (Exception ex) { }
            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableUsageInfoLogs"] != null)
                    EnableUsageInfoLogs = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableUsageInfoLogs"].Value);
            }
            catch (Exception ex) { }
            try
            {
                if (EnableUsageInfoLogs && config.AppSettings.Settings["NCacheServer.EnableUsageInfoLogsForTesting"] != null)
                    EnableUsageInfoLogsForTesting = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableUsageInfoLogsForTesting"].Value);
            }
            catch (Exception ex) { }
          
            try
            {
                if (config.AppSettings.Settings["NCacheServer.UrlUsageInfo"] != null)
                    UrlUsageInfo = config.AppSettings.Settings["NCacheServer.UrlusageInfo"].Value.ToString();
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.FeatureDataCollectionDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.FeatureDataCollectionDelay"].Value);

                    if (tempInt > 0)
                        FeatureDataCollectionInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.InitialLoggingDelayAtStartup"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.InitialLoggingDelayAtStartup"].Value);

                    if (tempInt > 0)
                        InitialLoggingDelayAtStartup = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.FeatureDataPostingDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.FeatureDataPostingDelay"].Value);

                    if (tempInt > 0)
                        FeatureDataPostingDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableFeatureUsageLogging"] != null)
                {
                    tempBool = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableFeatureUsageLogging"].Value);
                    EnableFeatureUsageLogging = tempBool;
                }
            }
            catch (Exception ex) { }


            try
            {
                if (config.AppSettings.Settings["NCacheServer.UploadFeatureUsageStats"] != null)
                {
                    tempBool = bool.Parse(config.AppSettings.Settings["NCacheServer.UploadFeatureUsageStats"].Value);

                    UploadFeatureUsageStats = tempBool;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.UsageFailureRetriesCount"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.UsageFailureRetriesCount"].Value);

                    if (tempInt > 0)
                        UsageFailureRetriesCount = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.InitialFeatureDataPostingDelay"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.InitialFeatureDataPostingDelay"].Value);

                    if (tempInt > 0 && tempInt <= _maxFeatureDataPostingDelay)
                        InitailFeatureDataPostingDelay = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.FeatureDataPostingWaitTime"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.FeatureDataPostingWaitTime"].Value);
                    if (tempInt > 0 && tempInt <= _maxFeatureDataPostingDelay)
                        FeatureDataPostingWaitTime = tempInt;
                }
            }
            catch (Exception ex) { }
        }

    
        public static int Port
        {
            get { return _port; }
            private set { _port = value; }
        }

        public static int ManagementPort
        {
            get { return _managementPort; }
            private set { _managementPort = value; }
        }

        public static int SendBufferSize
        {
            get { return _sendBufferSize; }
            private set { _sendBufferSize = value; }
        }

        public static int ReceiveBufferSize
        {
            get { return _receiveBufferSize; }
            private set { _receiveBufferSize = value; }
        }

        public static string UninstallInProgress
        {
            get { return _unInstallInProgress; }
            private set { _unInstallInProgress = value; }
        }

        public static bool LicenseLogging
        {
            get { return _licenseLogging; }
            private set { _licenseLogging = value; }
        }

        public static IPAddress BindToClientServerIP
        {
            get { return _bindToClientServerIP; }
            private set { _bindToClientServerIP = value; }
        }

        public static bool EnableDualSocket
        {
            get { return _enableDualSocket; }
            private set { _enableDualSocket = value; }
        }

        public static bool EnableNagling
        {
            get { return _enableNagling; }
            private set { _enableNagling = value; }
        }

        public static int NaglingSize
        {
            get { return _naglingSize; }
            private set { _naglingSize = value; }
        }

        public static DependencyLibrary DependencyLibrary
        {
            get { return _dependencyLibrary; }
            private set { _dependencyLibrary = value; }
        }
        public static string CacheUserName
        {
            get { return _cacheUserName; }
            private set { _cacheUserName = value; }
        }

        public static string CacheUserPassword
        {
            get { return _cacheUserPassword; }
            private set { _cacheUserPassword = value; }
        }

        public static bool EnableDebuggingCounters
        {
            get { return _enableDebuggingCounters; }
            private set { _enableDebuggingCounters = value; }
        }

        public static bool EnableBadClientDetection
        {
            get { return _enableBadClientDetection; }
            private set { _enableBadClientDetection = value; }
        }

        public static int ClientSocketSendTimeout
        {
            get { return _clientSocketSendTimeOut; }
            private set { _clientSocketSendTimeOut = value; }
        }

        public static int EventPriorityRatio
        {
            get { return _eventPriorityRatio; }
            private set { _eventPriorityRatio = value; }
        }

        public static int EventBulkCount
        {
            get { return _eventBulkCount; }
            private set { _eventBulkCount = value; }
        }

        public static int BulkEventCollectionInterval
        {
            get { return _bulkEventCollectionInterval; }
            private set { _bulkEventCollectionInterval = value; }
        }

        public static int ExpirationBulkRemoveSize
        {
            get { return _expirationBulkRemoveSize; }
            private set { _expirationBulkRemoveSize = value; }
        }

        public static int ExpirationBulkRemoveDelay
        {
            get { return _expirationBulkRemoveDelay; }
            private set { _expirationBulkRemoveDelay = value; }
        }

        public static int EvictionBulkRemoveSize
        {
            get { return _evictionBulkRemoveSize; }
            private set { _evictionBulkRemoveSize = value; }
        }

        public static int EvictionBulkRemoveDelay
        {
            get { return _evictionBulkRemoveDelay; }
            private set { _evictionBulkRemoveDelay = value; }
        }

        public static int BulkItemsToReplicate
        {
            get { return _bulkItemsToReplicate; }
            private set { _bulkItemsToReplicate = value; }
        }

        public static bool EnableCacheLastAccessCount
        {
            get { return _enableCacheLastAccessCount; }
            private set { _enableCacheLastAccessCount = value; }
        }

        public static bool EnableCacheLastAccessCountLogging
        {
            get { return _enableCacheLastAccessCountLogging; }
            private set { _enableCacheLastAccessCountLogging = value; }
        }
        public static int CacheLastAccessCountInterval

        {
            get { return _cacheLastAccessCountInterval; }
            private set { _cacheLastAccessCountInterval = value; }
        }

        public static int CacheLastAccessLogInterval
        {
            get { return _cacheLastAccessLogInterval; }
            private set { _cacheLastAccessLogInterval = value; }
        }

        public static int LOHPoolSize
        {
            get { return _lOHPoolSize; }
            private set { _lOHPoolSize = value; }
        }


        public static int LOHPoolBufferSize
        {
            get { return _lOHPoolBufferSize; }
            private set { _lOHPoolBufferSize = value; }
        }

        public static int CacheSizeThreshold
        {
            get { return _cacheSizeThreshold; }
            private set { _cacheSizeThreshold = value; }
        }

        public static int CacheSizeReportInterval
        {
            get { return _cacheSizeReportInterval; }
            private set { _cacheSizeReportInterval = value; }
        }

        public static bool LogClientEvents
        {
            get { return _logClientEvents; }
            private set { _logClientEvents = value; }
        }

        public static string EventLogLevel
        {
            get { return _eventLogLevel; }
            private set 
            { 
                _eventLogLevel = value;
                EventLogUtil.SetEventLogLevent();
            }
        }

        public static bool EnableForcedGC
        {
            get { return _enableForcedGC; }
            private set { _enableForcedGC = value; }
        }

        public static int ForcedGCThreshold
        {
            get { return _forcedGCThreshold; }
            private set { _forcedGCThreshold = value; }
        }

        public static int ForcedGCInterval
        {
            get { return _forcedGCInterval; }
            private set { _forcedGCInterval = value; }
        }

        public static bool EnableLogs
        {
            get { return _enableLogs; }
            private set 
            { 
                _enableLogs = value;
                NCacheLog.OnChange();
            }
        }

        public static bool EnableDetailedLogs
        {
            get { return _enableDetailedLogs; }
            private set 
            { 
                _enableDetailedLogs = value;
                NCacheLog.OnChange();
            }
        }

        public static int BufferSize
        {
            get { return _bufferSize; }
            private set { _bufferSize = value; }
        }

        public static bool AllowRequestEnquiry
        {
            get { return _allowRequestEnquiry; }
            private set { _allowRequestEnquiry = value; }
        }

        public static int RequestEnquiryInterval
        {
            get { return _requestEnquiryInterval; }
            private set { _requestEnquiryInterval = value; }
        }

        public static int RequestEnquiryRetries
        {
            get { return _requestEnquiryRetries; }
            private set { _requestEnquiryRetries = value; }
        }

        public static string NCacheSQLNotificationService
        {
            get { return _nCacheSQLNotificationService; }
            private set { _nCacheSQLNotificationService = value; }
        }

        public static string NCacheSQLNotificationQueue
        {
            get { return _nCacheSQLNotificationQueue; }
            private set { _nCacheSQLNotificationQueue = value; }
        }

        public static int ResponseDataSize
        {
            get { return _responseDataSize; }
            private set { _responseDataSize = value; }
        }

        public static int EnableSnapshotPoolingCacheSize
        {
            get { return _enableSnapshotPoolingCacheSize; }
            private set { _enableSnapshotPoolingCacheSize = value; }
        }

        public static int SnapshotPoolSize
        {
            get { return _snapshotPoolSize; }
            private set { _snapshotPoolSize = value; }
        }

        public static int SnapshotCreationThreshold
        {
            get { return _snapshotCreationThreshold; }
            private set { _snapshotCreationThreshold = value; }
        }

        public static int EnumeratorChunkSize
        {
            get { return _enumeratorChunkSize; }
            private set { _enumeratorChunkSize = value; }
        }

        public static bool EventsPersistence
        {
            get { return ServiceConfiguration._eventsPersistence; }
            private set { ServiceConfiguration._eventsPersistence = value; }
        }

        public static int EventsPersistenceInterval
        {
            get { return ServiceConfiguration._eventsPersistenceInterval; }
            private set { ServiceConfiguration._eventsPersistenceInterval = value; }
        }

        public static bool EnableServerCounters
        {
            get { return ServiceConfiguration._enableServerCounters; }
            private set { ServiceConfiguration._enableServerCounters = value; }
        }

        public static int MaxPendingConnections
        {
            get { return ServiceConfiguration._maxPendingConnections; }
            private set { ServiceConfiguration._maxPendingConnections = value; }
        }

        public static int MaxResponseLength
        {
            get { return ServiceConfiguration._maxResponseLength; }
            private set { ServiceConfiguration._maxResponseLength = value; }
        }

        public static int BlockingActivity
        {
            get { return ServiceConfiguration._blockingActivity; }
            private set { ServiceConfiguration._blockingActivity = value; }
        }

        public static int GracefullTimeout
        {
            get { return ServiceConfiguration._gracefullTimeout; }
            private set { ServiceConfiguration._gracefullTimeout = value; }
        }

        public static bool DisableIndexNotDefinedException
        {
            get { return ServiceConfiguration._disableIndexNotDefinedException; }
            private set { ServiceConfiguration._disableIndexNotDefinedException = value; }
        }

        public static float StateTransferDataSizePerSecond
        {
            get { return ServiceConfiguration._stateTransferDataSizePerSecond; }
            private set { ServiceConfiguration._stateTransferDataSizePerSecond = value; }
        }

        public static bool EnableGCDuringStateTransfer
        {
            get { return ServiceConfiguration._enableGCDuringStateTransfer; }
            private set { ServiceConfiguration._enableGCDuringStateTransfer = value; }
        }

        public static long GCThreshold
        {
            get { return ServiceConfiguration._gCThreshold; }
            private set { ServiceConfiguration._gCThreshold = value; }
        }

        public static bool EnableGCCollection
        {
            get { return ServiceConfiguration._enableGCCollection; }
            private set { ServiceConfiguration._enableGCCollection = value; }
        }

        public static int PreparedQueryTableSize
        {
            get { return ServiceConfiguration._preparedQueryTableSize; }
            private set { ServiceConfiguration._preparedQueryTableSize = value; }
        }

        public static int PreparedQueryEvictionPercentage
        {
            get { return ServiceConfiguration._preparedQueryEvictionPercentage; }
            private set { ServiceConfiguration._preparedQueryEvictionPercentage = value; }
        }

        public static long LicenseCheckInterval
        {
            get { return ServiceConfiguration._licenseCheckInterval; }
            private set { ServiceConfiguration._licenseCheckInterval = value; }
        }

        public static bool TurnOffRandomStartTime
        {
            get { return turnOffRandomStartTime; }
        }

        public static bool SimulateSocketClose
        {
            get { return ServiceConfiguration._simulateSocketClose; }
            private set { ServiceConfiguration._simulateSocketClose = value; }
        }

        public static int SocketCloseInterval
        {
            get { return ServiceConfiguration._socketCloseInterval; }
            private set { ServiceConfiguration._socketCloseInterval = value; }
        }

        public static bool UseAvgStats
        {
            get { return ServiceConfiguration._useAvgStats; }
            private set { ServiceConfiguration._useAvgStats = value; }
        }

        public static bool AsyncTcpUpQueue
        {
            get { return ServiceConfiguration._asyncTcpUpQueue; }
            private set { ServiceConfiguration._asyncTcpUpQueue = value; }
        }

        public static bool PrintClusterStats
        {
            get { return ServiceConfiguration._printClusterStats; }
            private set { ServiceConfiguration._printClusterStats = value; }
        }

        public static TimeSpan StatsPrintInterval
        {
            get { return ServiceConfiguration._statsPrintInterval; }
            private set { ServiceConfiguration._statsPrintInterval = value; }
        }

        public static bool EnableDebugLog
        {
            get { return ServiceConfiguration._enableDebugLog; }
            private set { ServiceConfiguration._enableDebugLog = value; }
        }
        
        public static int CacheStartDelay
        {
            get { return ServiceConfiguration._cacheStartDelay; }
            private set { ServiceConfiguration._cacheStartDelay = value; }
        }
        
        public static int AutoStartDelay
        {
            get { return ServiceConfiguration._autoStartDelay; }
            private set { ServiceConfiguration._autoStartDelay = value; }
        }

        public static string ManagementEndPoint
        {
            get { return ServiceConfiguration._mgmtEndPoint; }
            private set { ServiceConfiguration._mgmtEndPoint = value; }
        }

        public static string ServerEndPoint
        {
            get { return ServiceConfiguration._serverEndPoint; }
            private set { ServiceConfiguration._serverEndPoint = value; }
        }

        public static int GCCollectInterval
        {
            get { return ServiceConfiguration._gCCollectInterval; }
            private set { ServiceConfiguration._gCCollectInterval = value; }
        }

        public static decimal ClusterHealthDetectionInterval
        {
            get { return ServiceConfiguration._clusterHealthDetectionInterval; }
            private set { ServiceConfiguration._clusterHealthDetectionInterval = value; }
        }

        
        public static int DataChunkSize
        {
            get { return _dataChunkSize; }
            set { _dataChunkSize = value; }
        }

        public static int NotificationEventInterval
        {
            get { return _notificationEventInterval; }
            set { _notificationEventInterval = value; }
        }

        public static int ManagementPortUpper
        {
            get { return ServiceConfiguration._managementPortUpper; }
            private set { ServiceConfiguration._managementPortUpper = value; }
        }
        public static int ManagementPortLower
        {
            get { return ServiceConfiguration._managementPortLower; }
            private set { ServiceConfiguration._managementPortLower = value; }
        }

        public static double CommandExecutionThreshold
        {
            get { return _commandExecutionThreshold; }
            private set { _commandExecutionThreshold = value; }
        }
        public static bool EnableRequestCancellation
        {
            get { return _enableRequestCancellation; }
            private set { _enableRequestCancellation = value; }
        }
        public static bool EnableCommandThresholdLogging
        {
            get { return _enableCommandThresholdLogging; }
            private set { _enableCommandThresholdLogging = value; }
        }

        public static int PerfmonInterval
        {
            get { return _interval; }
            set { _interval = value; }
        }
        public static bool PerfmonArchive
        {
            get { return _archive; }
            private set { _archive = value; }
        }

        public static string PerfmonPath
        {
            get { return _perfmonPath; }
            private set { _perfmonPath = value; }
        }

        /// <summary>
        /// GC collection interval in minutes
        /// </summary>
        public static int ServiceGCInterval
        {
            get { return _serviceGCInterval; }
            set { _serviceGCInterval = value; }
        }

        public static bool PerfmonEnable
        {
            get { return _enable; }
            private set {
                bool prevValue = _enable;
                _enable = value;
                if (prevValue!= _enable)
                {
                    _perfmonHasChange = true;
                }

            }
        }
       
        public static bool HasPerfmonChanged
        {
            get { return _perfmonHasChange; }
            set { _perfmonHasChange = value; }
        }

        public static string NosDBDependencyProviderVersion
        {
            get { return _nosdbDependencyProviderVersion; }
            set { _nosdbDependencyProviderVersion = value; }
        }

        public static int ServerFailureRetryDelayInterval
        {
            get { return _nodeFailureRetryDelayInterval; }
        }

        public static bool PublishCountersToCacheHost
        {
            get { return _publishCountersToCacheHost; }
            private set { _publishCountersToCacheHost = value; }
        }


        public static long CollectionChunkSize
        {
            get { return _collectionChunkSize; }
            private set { _collectionChunkSize = value; }
        }
        public static bool AutoRenewal
        {
            get { return LoadAutoRenewal(); }

        }
        public static TimeSpan PubSubClientInactivityTimeout
        {

            get { return ServiceConfiguration._pubSubClientInactivityTimeout; }
            private set { ServiceConfiguration._pubSubClientInactivityTimeout = value; }
        }
        public static int GetLoaderServicePort
        {
            get
            {

                try
                {
                    string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                    string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                    string serviceEXE3 = "";
#if NETCORE
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                        serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.LoaderService.dll");
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                        serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.LoaderDaemon.dll");
#else
                    serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.LoaderService.exe");
#endif
                    System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
                    if (config.AppSettings.Settings["LoaderPort"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["LoaderPort"].Value);
                        if (tempInt > 1024 && tempInt < UInt16.MaxValue)
                            loaderServicePort = tempInt;
                    }

                }
                catch (Exception){}
                return loaderServicePort;

            }
        }

        public static bool EnableObjectPooling
        {
            get { return _enableObjectPooling; }
            private set { _enableObjectPooling = value; }
        }

        /// <summary>
        /// Flag in service configuration to determine auto start behavior of web manager.
        /// </summary>
        public static bool EnableAutoStartWebManagement
        {
            get { return _enableAutoStartWebManagement; }
            private set { _enableAutoStartWebManagement = value; }
        }

        /// <summary>
        /// Retries in service configuration to determine number of retries to ping web manager.
        /// </summary>
        public static int WebManagerStartRetries
        {
            get { return _webManagerStartRetries; }
            private set { _webManagerStartRetries = value; }
        }

        public static int TransactionalPoolCapacity
        {
            get { return _transactinalPoolCapacity; }
        }

        public static int PauseWriterThreshold
        {
            get { return ServiceConfiguration._pauseWriterThreshold; }
        }

     
        public static string UrlUsageInfo { get => _urlUsageInfo; set => _urlUsageInfo = value; }

        public static void LoadHotApplicableConfigurations()
        {
            System.Configuration.Configuration config;
            try
            {
                    string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                    string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                    string serviceEXE3 = "";
#if NETCORE
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                        serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_WIN);
                    if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                        serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_UNIX);
#else
                serviceEXE3=Path.Combine(serviceEXE2,"Alachisoft.NCache.Service.exe");
#endif
                    config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
                
            }
            catch (Exception ex) { return; }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"] != null)
                {
                    double tempdouble = double.Parse(config.AppSettings.Settings["NCacheServer.CommandExecutionThreshold"].Value);
                    if (tempdouble > 0)
                        CommandExecutionThreshold = tempdouble;
                }
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"] != null)
                {
                    bool tempbool = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableCommandThresholdLogging"].Value);

                    EnableCommandThresholdLogging = tempbool;
                }
            }
            catch (Exception ex) { }
        }
        
        public static void ChangePerfmonLogPath (string path)
        {
            System.Configuration.Configuration config;
            try
            {
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = "";
#if NETCORE
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
                        serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_WIN);
                if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                        serviceEXE3 = Path.Combine(serviceEXE2, NETCORE_SERVICE_UNIX);
#else
                serviceEXE3=Path.Combine(serviceEXE2,"Alachisoft.NCache.Service.exe");
#endif
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);     
            }
            catch (Exception ex) { return; }
            try
            {
                if (path != null)
                {
                    if (config.AppSettings.Settings["NCacheServer.PerfmonLogPath"] != null)
                    {
                        PerfmonPath = path; 
                    }
                }
            }
            catch (Exception ex) { PerfmonPath = System.IO.Path.Combine(AppUtil.InstallDir, "log-files\\PerfmonLogsstatistics\\"); }
        }
    }
}