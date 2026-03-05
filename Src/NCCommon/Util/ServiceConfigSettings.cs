using Alachisoft.NCache.Common.Enum;
using System;
using System.Collections.Generic;
using System.Net;

namespace Alachisoft.NCache.Common.Util
{
    public class ServiceConfigSettings
    {
        private static Dictionary<string, ConfigSetting> _serviceSettingsMap = new Dictionary<string, ConfigSetting>();

        static ServiceConfigSettings()
        {
            #region NOT HOT APPLICABLE
            _serviceSettingsMap.Add(ServiceConfigKeys.UseCustomThreadPool, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.CustomPipeliningThreadPoolFactor, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.MetricsPersistenceInterval, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ThreadsPerProcessor, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.CommunicationMechanism, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ExecuteOperations, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.UseCommandThreadPool, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.SendBufferSize, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableHeartbeat, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.SkippedHeartbeatThresholdInterval, new ConfigSetting(DataType.DOUBLE, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.SkippedHeartbeatEventLogInterval, new ConfigSetting(DataType.DOUBLE, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.RandomizeFirstHeartbeatRequest, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableUsageInfoLogs, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableSnmpMonitoring, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableSystemCountersMonitoring, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.CachingModuleTotalBuckets, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableUsageInfoLogsForTesting, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ConnectedClientsPersistorInterval, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.HideOperatingSystem, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ReceiveBufferSize, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.BindToIP, new ConfigSetting(DataType.IPADDRESS, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableDualSocket, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.NCacheSQLNotificationService, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.NCacheSQLNotificationQueue, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ModuleStateTransferBufferSize, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ServiceGCCollectionInterval, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableObjectPooling, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.TransactionalPoolCapacity, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.Port, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ManagementPort, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ManagementPortLower, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ManagementPortUpper, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.MetricServerPort, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.SnmpListenersInfoPort, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.AllowMMapDirectory, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.CredentialsEncryptionKey, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.KeyEncryptionKey, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxMessagesPerPoll, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.PerfmonEnable, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.PubSubSequenceTimeout, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.ResponseDataSize, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.StartWebManagement, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.StateTransferDataSizePerThread, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.WebManagerStartRetries, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.PublicIP, new ConfigSetting(DataType.STRING, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.AutoRenewal, new ConfigSetting(DataType.BOOLEAN, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.HeartbeatRequestTimeout, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxHeartbeatFailureDuration, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.CloudHeartbeatCheckInterval, new ConfigSetting(DataType.LONG, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.HeartbeatRetryDelaySeconds, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxHeartbeatRetries, new ConfigSetting(DataType.INTEGER, false));
            _serviceSettingsMap.Add(ServiceConfigKeys.StubWritingInterval, new ConfigSetting(DataType.INTEGER, false));

            #endregion

            #region HOT APPLICABLE
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxAccumulatedCacheSize, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LicenseLogging, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.TouchInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ReplicaJoinDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableNagling, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.NagglingSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableDebuggingCounters, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableBadClientDetection, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ClientSocketSendTimeOut, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventPriorityRatio, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventBulkCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BulkEventCollectionInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ExpirationBulkRemoveSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ExpirationBulkRemoveDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EvictionBulkRemoveSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EvictionBulkRemoveDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BulkItemsToReplicate, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableCacheLastAccessCount, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableCacheLastAccessCountLogging, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CacheLastAccessCountInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CacheLastAccessLogInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LOHPoolSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LOHPoolBufferSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CacheSizeThreshold, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CacheSizeReportInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxRequestPerSecPerCacheInDEV, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LogClientEvents, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventLogLevel, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableForcedGC, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ForcedGCThreshold, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ForcedGCInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableLogs, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableDetailedLogs, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BufferSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.AllowClusterRequestEnquiry, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ClusterRequestEnquiryInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ClusterRequestEnquiryRetries, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableSnapshotPoolingCacheSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SnapshotPoolSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SnapshotCreationThreshold, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnumeratorChunkSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.UninstallInProgress, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventsPersistence, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxPendingConnections, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableServerCounters, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventsPersistenceInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxResponseLength, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BlockingActivityTimeout, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.GracefullShutdownTimeout, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.DisableIndexNotDefinedException, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.StateTransferDataSizePerSecond, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableGCDuringStateTransfer, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.GCThreshold, new ConfigSetting(DataType.LONG, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableGCCollection, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.preparedQueryTableSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.preparedQueryEvictionPercentage, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LicenseCheckInterval, new ConfigSetting(DataType.LONG, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LicenseAutoRenewalInterval, new ConfigSetting(DataType.LONG, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.TurnOffRandomStartTime, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.LicenseInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.AcquireClientLicenseRetries, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.AcquireClientLicenseRetryInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SimulateSocketClose, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SocketCloseInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.useAvgStats, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.asyncTcpUpQueue, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.printClusterStats, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.statsPrintInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableDebugLog, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CacheStartDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.AutoStartDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MgmtEndPoint, new ConfigSetting(DataType.IPADDRESS_PORT, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CommandExecutionThreshold, new ConfigSetting(DataType.DOUBLE, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableCommandThresholdLogging, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PerfmonInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PerfmonArchive, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PerfmonLogPath, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ServerEndPoint, new ConfigSetting(DataType.IPADDRESS_PORT, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.GCCollectInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ClusterHealthDetectionInterval, new ConfigSetting(DataType.DECIMAL, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CancelTimedOutRequests, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.DataChunkSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.NotificationEventInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.NosDBDependencyProviderVersion, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ServerFailureRetryDelayInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.ReaderExpiration, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EventThreadPoolCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CollectionChunkSize, new ConfigSetting(DataType.LONG, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PublishCountersToCacheHost, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PubSubClientInactivityTimeout, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SystemTopicsPriority, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MetricsMonitorPublishingInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableLogsMonitoring, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableMetricsPublishing, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableAPILogsMonitoring, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableEventsMonitoring, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableClusterHealthMonitoring, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableStatsMonitoring, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PauseWriterThreshold, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BridgeOperationFailureRetries, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BridgeReplicationInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.BridgeBulkOperationSize, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.FeatureDataCollectionDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.InitialLoggingDelayAtStartup, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.FeatureDataPostingDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EvalFeatureDataPostingDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableFeatureUsageLogging, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.UploadFeatureUsageStats, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.UsageFailureRetriesCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.InitialFeatureDataPostingDelay, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.FeatureDataPostingWaitTime, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PubSubOrderedMessagesTimeout, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxCacheCountInDEV, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxAccumulatedCacheSizeInDEV, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MaxStressTestTasks, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PersistenceStoreCleanupInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MSSQLStoreCleanupInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PersistenceStoreDataRetentionTime, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.MSSQLStoreDataRetentionTime, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.EnableCredentialCaching, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.SearchCountersFetchInterval, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.CollectSystemMetrics, new ConfigSetting(DataType.BOOLEAN, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PubSubStoreBucketsCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.TopicDeletionThrottleLimit, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.TopicProcessingCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.DeleteMessageCount, new ConfigSetting(DataType.INTEGER, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.PersistenceDir, new ConfigSetting(DataType.STRING, true));
            _serviceSettingsMap.Add(ServiceConfigKeys.StateTransferChunkSize, new ConfigSetting(DataType.INTEGER, true));
            #endregion
        }

        public static ConfigSetting ResolveConfigSettings(string key)
        {
            ConfigSetting configSetting = null;

            if (_serviceSettingsMap.TryGetValue(key, out configSetting))
            {
                return configSetting;
            }

            return null;
        }

        public static bool ValidateDataType(DataType KeyDataType, string value)
        {
            switch (KeyDataType)
            {
                case DataType.INTEGER:
                    return int.TryParse(value, out _);

                case DataType.DOUBLE:
                    return double.TryParse(value, out _);

                case DataType.LONG:
                    return long.TryParse(value, out _);

                case DataType.BOOLEAN:
                    return bool.TryParse(value, out _);

                case DataType.IPADDRESS:
                    return IPAddress.TryParse(value, out _);

                case DataType.IPADDRESS_PORT:
                    string[] mappingAddress = value.Split(':');
                    if (mappingAddress.Length == 2)
                    {
                        IPAddress ipAddress;
                        int port;

                        bool ipValid = IPAddress.TryParse(mappingAddress[0], out ipAddress);
                        bool portValid = int.TryParse(mappingAddress[1], out port);

                        return ipValid && portValid;
                    }
                    else
                        return false;

                case DataType.STRING:
                    return true;

                default:
                    return false;
            }
        }
    }
}
