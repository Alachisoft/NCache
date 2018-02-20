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
// limitations under the License

using System;
using System.Configuration;
using System.Net;
using System.IO;
using System.Diagnostics;

namespace Alachisoft.NCache.Common.Util
{
    public class ServiceConfiguration
    {
        private static int MB = 1024 * 1024;
        private static int _port = 9800;
        private static int _managementPort = 8250;
        private static int _sendBufferSize = 131072;
        private static bool _licenseLogging = false;
        private static int _receiveBufferSize = 131072;
        private static IPAddress _bindToIP;
        private static bool _enableDualSocket = false;
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
        private static int _cacheLastAccessLogInterval = 40;
        private static int _lOHPoolSize = 40;
        private static int _lOHPoolBufferSize = 512;
        private static int _cacheSizeThreshold = 80;
        private static int _cacheSizeReportInterval = 15; //minutes
        private static bool _logClientEvents = false;
        private static string _eventLogLevel;
     
        private static bool _enableLogs = false;
        private static bool _enableDetailedLogs = false;
        private static int _bufferSize = 1;
        private static bool _allowRequestEnquiry = false;
        private static int _requestEnquiryInterval = 10;//s
        private static int _requestEnquiryRetries = 1;
        private static string _nCacheSQLNotificationService;
        private static string _nCacheSQLNotificationQueue;
        private static int _responseDataSize = 1024 * 1024 * 1024; //1024 MB's
        private static int _enableSnapshotPoolingCacheSize = 100000; //number of item
        private static int _snapshotPoolSize = 10;
        private static int _snapshotCreationThreshold = 30; //seconds
        private static int _enumeratorChunkSize = 1000;
        private static int _requestInquiryCleanInterval = 10; // seconds 
        private static bool _enableRequestInquiry = false;
        private static bool _eventsPersistence = false;
        private static int _eventsPersistenceInterval = 10;
        private static int _maxPendingConnections = 10;
        private static bool _enableServerCounters = true;
        private static int _maxResponseLength = 1;
        private static int _gracefullTimeout = 1;
        private static int _blockingActivity = 1;
        private static bool _disableIndexNotDefinedException = false;
        private static bool _enableDataSourceCounters = true;
        private static float _stateTransferDataSizePerSecond = 10;
        private static bool _enableGCDuringStateTransfer = false;
        private static long _gCThreshold = 1024 * MB * 2;
        private static bool _enableGCCollection = true;
        private static int _preparedQueryTableSize = 1000;
        private static int _preparedQueryEvictionPercentage = 10;
        private static double _licenseCheckInterval = 24 * 60 * 60 * 1000;
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
        private static string _mgmtEndPoint = "";
        private static string _serverEndPoint = "";
        private static decimal _clusterHealthDetectionInterval = 3;
        private static int _dataChunkSize = 512 * 1024;
        private static int _managementPortLower = 8300;
        private static int _managementPortUpper = 8400;
        private static string tempString;
        private static int tempInt;
        private static bool tempBool;
        private static long tempLong;
        private static decimal tempDecimal;
        private static TimeSpan tempTimeSpan;
        private static double _commandExecutionThreshold = 5; // seconds
        private static bool _enableCommandThresholdLogging = false;
        private static int _touchInterval = 5; // seconds

        /// <summary>
        /// Touch interval in seconds
        /// </summary>
        public static int ItemTouchInterval
        {
            get { return _touchInterval; }
            set { _touchInterval = value; }
        }

        private static int _serviceGCInterval = 180; //in minutes

        static bool isHotApply = false;
        
        private static string _nosdbDependencyProviderVersion = "1.0.0.0";
        private static int _nodeFailureRetryDelayInterval = 2000;//in msec


        private static int _threadsPerProcessor = 2;
        private static int _readerExpiration = -1;
        private static int _eventThreadPoolCount = 1;
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


        static ServiceConfiguration()
        {
            Load();
        }

        public static void Load()
        {
            System.Configuration.Configuration config;
#if !NETCORE && !NETCOREAPP2_0
            try
            {

                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Service.exe");
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
            }
            catch (Exception ex) { return; }
#else
            try
            {
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Daemon.dll");
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
            }
            catch (Exception ex) { return; }

#endif

#if JAVA
                if (config.AppSettings.Settings["CacheServer.ExpirationBulkRemoveSize"] != null)
                    _expirationBulkRemoveSize = config.AppSettings.Settings["CacheServer.ExpirationBulkRemoveSize"].Value;

                if (config.AppSettings.Settings["CacheServer.ExpirationBulkRemoveDelay"] != null)
                    _expirationBulkRemoveDelay = config.AppSettings.Settings["CacheServer.ExpirationBulkRemoveDelay"].Value;

                if (config.AppSettings.Settings["CacheServer.EvictionBulkRemoveSize"] != null)
                    _evictionBulkRemoveSize = config.AppSettings.Settings["CacheServer.EvictionBulkRemoveSize"].Value;

                if (config.AppSettings.Settings["CacheServer.EvictionBulkRemoveDelay"] != null)
                    _evictionBulkRemoveDelay = config.AppSettings.Settings["CacheServer.EvictionBulkRemoveDelay"].Value;
#else
            if (!isHotApply)
            {

                try
                {
#if JAVA
                        if (config.AppSettings.Settings["CacheServer.ThreadsPerProcessor"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.ThreadsPerProcessor"].Value);
#else
                    if (config.AppSettings.Settings["NCacheServer.ThreadsPerProcessor"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ThreadsPerProcessor"].Value);
#endif
                        if (tempInt > 0)
                            _threadsPerProcessor = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
#if JAVA
                        if (config.AppSettings.Settings["CacheServer.Port"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.Port"].Value);
#else
                    if (config.AppSettings.Settings["NCacheServer.Port"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.Port"].Value);
#endif
                        if (tempInt > 0)
                            Port = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
#if JAVA
                        if (config.AppSettings.Settings["CacheServer.ManagementPort"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.ManagementPort"].Value);
#else
                    if (config.AppSettings.Settings["NCacheServer.ManagementPort"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ManagementPort"].Value);
#endif
                        if (tempInt > 0)
                            ManagementPort = tempInt;
                    }
                }
                catch (Exception ex) { }

                try
                {
#if JAVA
                        if (config.AppSettings.Settings["CacheServer.SendBufferSize"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.SendBufferSize"].Value);
#else
                    if (config.AppSettings.Settings["NCacheServer.SendBufferSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SendBufferSize"].Value);
#endif
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
#if JAVA
                        if (config.AppSettings.Settings["CacheServer.ReceiveBufferSize"] != null)
                        {
                            tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.ReceiveBufferSize"].Value);
#else
                    if (config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.ReceiveBufferSize"].Value);
#endif
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
                    if (config.AppSettings.Settings["NCacheServer.EnableDualSocket"] != null)
                        EnableDualSocket = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDualSocket"].Value);
                }
                catch (Exception ex) { }

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
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.EnableDebuggingCounters"] != null)
                        EnableDebuggingCounters = bool.Parse(config.AppSettings.Settings["CacheServer.EnableDebuggingCounters"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"] != null)
                    EnableDebuggingCounters = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDebuggingCounters"].Value);
#endif
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
                if (config.AppSettings.Settings["NCacheServer.EventsPersistence"] != null)
                    EventsPersistence = bool.Parse(config.AppSettings.Settings["NCacheServer.EventsPersistence"].Value);
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["Cache.MaxPendingConnections"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["Cache.MaxPendingConnections"].Value);
#else
                if (config.AppSettings.Settings["NCache.MaxPendingConnections"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCache.MaxPendingConnections"].Value);
#endif
                    if (tempInt > 10)
                        EventsPersistenceInterval = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["Cache.EnableServerCounters"] != null)
                        EnableServerCounters = bool.Parse(config.AppSettings.Settings["Cache.EnableServerCounters"].Value);
#else
                if (config.AppSettings.Settings["NCache.EnableServerCounters"] != null)
                    EnableServerCounters = bool.Parse(config.AppSettings.Settings["NCache.EnableServerCounters"].Value);
#endif
            }
            catch (Exception ex) { }

            try
            {
                if (config.AppSettings.Settings["NCacheServer.EventsPersistenceInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.EventsPersistenceInterval"].Value);
                    if (tempInt > 10)
                        MaxPendingConnections = tempInt;
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
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.BlockingActivityTimeout"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.BlockingActivityTimeout"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.BlockingActivityTimeout"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.BlockingActivityTimeout"].Value);
#endif
                    if (tempInt > 1)
                        BlockingActivity = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.GracefullShutdownTimeout"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.GracefullShutdownTimeout"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.GracefullShutdownTimeout"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.GracefullShutdownTimeout"].Value);
#endif
                    if (tempInt > 1)
                        GracefullTimeout = tempInt;
                }
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.DisableIndexNotDefinedException"] != null)
                        DisableIndexNotDefinedException = bool.Parse(config.AppSettings.Settings["CacheServer.DisableIndexNotDefinedException"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.DisableIndexNotDefinedException"] != null)
                    DisableIndexNotDefinedException = bool.Parse(config.AppSettings.Settings["NCacheServer.DisableIndexNotDefinedException"].Value);
#endif
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.EnableDataSourceCounters"] != null)
                        EnableDataSourceCounters = bool.Parse(config.AppSettings.Settings["CacheServer.EnableDataSourceCounters"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.EnableDataSourceCounters"] != null)
                    EnableDataSourceCounters = bool.Parse(config.AppSettings.Settings["NCacheServer.EnableDataSourceCounters"].Value);
#endif
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
#if JAVA
                    if (config.AppSettings.Settings["Cache.EnableGCCollection"] != null)
                        EnableGCCollection = bool.Parse(config.AppSettings.Settings["Cache.EnableGCCollection"].Value);
#else
                if (config.AppSettings.Settings["NCache.EnableGCCollection"] != null)
                    EnableGCCollection = bool.Parse(config.AppSettings.Settings["NCache.EnableGCCollection"].Value);
#endif
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
                    if (tempLong >= 10)
                        LicenseCheckInterval = tempLong * 60 * 60 * 1000;
                }
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                if (config.AppSettings.Settings["Cache.SimulateSocketClose"] != null)
                    SimulateSocketClose = bool.Parse(config.AppSettings.Settings["Cache.SimulateSocketClose"].Value);
#else
                if (config.AppSettings.Settings["NCache.SimulateSocketClose"] != null)
                    SimulateSocketClose = bool.Parse(config.AppSettings.Settings["NCache.SimulateSocketClose"].Value);
#endif
            }
            catch (Exception ex) { }

            try
            {
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.SocketCloseInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.SocketCloseInterval"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.SocketCloseInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.SocketCloseInterval"].Value);
#endif
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
#if JAVA
                    if (config.AppSettings.Settings["CacheServer.HeartbeatInterval"] != null)
                    {
                        tempInt = Int32.Parse(config.AppSettings.Settings["CacheServer.HeartbeatInterval"].Value);
#else
                if (config.AppSettings.Settings["NCacheServer.HeartbeatInterval"] != null)
                {
                    tempInt = Int32.Parse(config.AppSettings.Settings["NCacheServer.HeartbeatInterval"].Value);
#endif
                    if (tempInt > 0)
                        HeartbeatInterval = tempInt * 1000;
                }
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

#endif
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

        //public static string UninstallInProgress
        //{
        //    get { return _unInstallInProgress; }
        //    private set { _unInstallInProgress = value; }
        //}

        public static bool LicenseLogging
        {
            get { return _licenseLogging; }
            private set { _licenseLogging = value; }
        }

        public static IPAddress BindToIP
        {
            get { return _bindToIP; }
            private set { _bindToIP = value; }
        }


        public static bool EnableDualSocket
        {
            get { return _enableDualSocket; }
            private set { _enableDualSocket = value; }
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

        public static int RequestInquiryCleanInterval
        {
            get { return _requestInquiryCleanInterval; }
            private set { _requestInquiryCleanInterval = value; }
        }

        public static bool EnableRequestInquiry
        {
            get { return _enableRequestInquiry; }
            private set { _enableRequestInquiry = value; }
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

        public static bool EnableDataSourceCounters
        {
            get { return ServiceConfiguration._enableDataSourceCounters; }
            private set { ServiceConfiguration._enableDataSourceCounters = value; }
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

        public static double LicenseCheckInterval
        {
            get { return ServiceConfiguration._licenseCheckInterval; }
            private set { ServiceConfiguration._licenseCheckInterval = value; }
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

        public static int HeartbeatInterval
        {
            get { return ServiceConfiguration._heartbeatInterval; }
            private set { ServiceConfiguration._heartbeatInterval = value; }
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

        public static bool EnableCommandThresholdLogging
        {
            get { return _enableCommandThresholdLogging; }
            private set { _enableCommandThresholdLogging = value; }
        }

        /// <summary>
        /// GC collection interval in minutes
        /// </summary>
        public static int ServiceGCInterval
        {
            get { return _serviceGCInterval; }
            set { _serviceGCInterval = value; }
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

        public static void LoadHotApplicableConfigurations()
        {
            System.Configuration.Configuration config;
            try
            {
#if !NETCORE && !NETCOREAPP2_0
                string workingDir = Path.Combine(AppUtil.InstallDir, "bin");
                string exeDir = Path.Combine(workingDir, "service");
                string serviceExe = Path.Combine(exeDir, "Alachisoft.NCache.Service.exe");
                config = ConfigurationManager.OpenExeConfiguration(serviceExe);
#else
                string serviceEXE1 = Path.Combine(AppUtil.InstallDir, "bin");
                string serviceEXE2 = Path.Combine(serviceEXE1, "service");
                string serviceEXE3 = Path.Combine(serviceEXE2, "Alachisoft.NCache.Daemon.dll");
                config = ConfigurationManager.OpenExeConfiguration(serviceEXE3);
#endif
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
    }
}
