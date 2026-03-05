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
using System.Collections.Generic;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Licensing;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Topologies.Clustered;
using System.Net;
using Alachisoft.NCache.Management.Statistics;
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.Pooling.Stats;
using System.Diagnostics;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;
using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Cache.Caching.Statistics;

namespace Alachisoft.NCache.Management
{
    public interface ICacheServer : IDisposable, IMetricsMonitor, IWebStatistics, IPingable
    {
        string GetClusterIP();

        string GetLocalCacheIP();

        string Source { get; set; }

        void SetLocalCacheIP(string ip);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="buffer"></param>
        void CopyAssemblies(string cacheName, string assemblyFileName, byte[] buffer);


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="buffer"></param>
        void CopyAssemblies(string cacheName, Dictionary<string, byte[]> filesData);

        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        byte[] GetAssembly(string cacheName, string fileName);
       

        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <summary>
        void ClearCache(string cacheId);

        /// <summary>
        /// Get a list of running caches (local + clustered)
        /// </summary>
        /// <returns>list of running caches</returns>        
        ArrayList GetRunningCaches(string userId, string password);

        IDictionary GetCacheProps();
        /// <summary>
        /// A collection of the cache infos registered with the server.
        /// </summary>
        /// <remarks>
        /// CacheProps are in new format now. Instead of saving the props string,
        /// it now saves CacheServerConfig instance:
        /// 
        /// |local-cache-id               | CacheServerConfig instance
        /// |partitioned-replica-cache-id | IDictionary
        ///                               | replica-id  | CacheServerConfig instance
        /// </remarks>
        IDictionary CacheProps();

        CacheServerConfig GetCacheConfiguration(string cacheId);

        CacheInfo GetCacheInfo(string cacheId);

        string GetHostName();

        void ReloadSrvcConfig();

        int GetSocketServerPort();

        CacheRegisterationInfo GetUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining);

        /// <summary>
        /// Register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite,bool hotApply, ServerLicenseInfo licenseInfo = null, string registerAs = "");

        /// <summary>
        /// Adds Server Node
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="config"></param>
        /// <param name="partId"></param>
        /// <param name="overwrite"></param>
        /// <param name="hotApply"></param>
        /// <returns></returns>
        bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply, ServerLicenseInfo licenseInfo = null, string registerAs = "");

        NodeInfoMap GetNodeInfo();

        string CanApplyHotConfiguration(string cacheId, CacheServerConfig config);

        bool CanApplyHotConfiguration(string cacheId, long size);

        void RemoveCacheServerFromClientConfig(string cacheId, string serverName);       

        void RemoveCacheFromClientConfig(string cacheId);

        void UpdateClientServersList(string cacheId, Alachisoft.NCache.Management.ClientConfiguration.CacheServerList serversPriorityList, string serverRuntimeContext);
        
        void UpdateClientServersList(string cacheId, string[] servers, ref string xml, string clientCacheId, bool loadBalance);

        
        ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId);

        void UpdateClientConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration);

        string GetBindIP();

        int GetClientConfigId();

        ClientNodeStatusWrapper GetClientNodeStatus(string cacheId);

        ServerLicenseInfo GetServerLicenseInfo(bool ignoreMac = false);

        /// <summary>
        /// Disbale logging
        /// </summary>
        /// <param name="subsystem">Subsystem for which logging will be disabled</param>
        /// <param name="type">Type of logging to disable</param>
        void DisableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type);

        void EnableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type);

        void SynchronizeClientConfig();

        /// <summary>
        /// Update TCP cache settings that includes updated list of TCP members
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        bool ApplyCacheConfiguration(string cacheId, CacheServerConfig props, bool hotApply);

        /// <summary>
        /// Un-register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        void UnregisterCache(string cacheId, string partId, bool removeServerOnly, bool removePersistedData = false);        

        void StartCache(string cacheId);       

        void StartCache(string cacheId, string partitionId);
        void StartCache(string cacheId, string partitionId, bool twoPhaseInitialization, ServerLicenseInfo licenseInfo = null, string registerAs = "");
        void StartCachePhase2(string cacheId);

        void StartCache(string cacheId, ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate);

        /// <summary>
        /// Start a cache and provide call backs
        /// </summary>
        /// <param name="cahcheID"></param>
        /// <param name="propertyString"></param>
        /// <param name="itemAdded"></param>
        /// <param name="itemRemoved"></param>
        /// <param name="itemUpdated"></param>
        /// <param name="cacheCleared"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        void StartCache(string cacheId, string partitionId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate,
            bool twoPhaseInitialization, ServerLicenseInfo licenseInfo = null, string registerAs = "");

        void StopCache(string cacheId);
       
        void StopCache(string cacheId, string partitionId);

        Hashtable DetectNICs();

        void BindToIP(BindedIpMap bindIPMap);
        
        BindedIpMap BindedIp();

        int GetMaxPort();
     
        int GetMaxSocketPort();
     
        CacheStatusOnServerContainer IsClusteredCache(string cacheId);

        bool IsPortAvailable(int port, string cacheName);

        bool NodeIsAllowed(int port, string id);
     
        StatusInfo GetCacheStatus(string cacheId, string partitionId);
        
        void StartMonitoringActivity();
  
        void StopMonitoringActivity();

        void PublishActivity();

        void MakeCacheActive(string cacheId,bool active);

        void ClearCacheContent(string cacheId);
   
        bool IsRunning(string cacheId);
      
        Alachisoft.NCache.Caching.Statistics.CacheStatistics GetStatistics(string cacheId);

        long GetCacheCount(string cacheId);

        void BalanceDataloadOnCache(string cacheId);

        bool IsCacheRegistered(string cacheId);

        Node[] GetCacheServers(string cacheId);

        ConfiguredCacheInfo[] GetAllConfiguredCaches();

        CacheNodeStatistics[] GetCacheStatistics(string cacheId);

        Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId);


        string GetLicenseKey();

        Hashtable GetSnmpPorts();

        void StopServer();
        bool IsSendingDumpCompleted();

        string GetServerPlatform();

        Alachisoft.NCache.Config.NewDom.CacheServerConfig GetNewConfiguration(string cacheId);

        bool ApplyCacheConfiguration(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig props, bool hotApply);

        NewCacheRegisterationInfo GetNewUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining);

        List<Alachisoft.NCache.Common.Monitoring.ClientNode> GetCacheClients(string cacheId);

        List<Alachisoft.NCache.Common.Monitoring.ClientProcessStats> GetClientProcessStats(string cacheId);

        Common.ProductVersion GetProductVersion();

        int GetShutdownTimeout();
       
        bool IsTayzGridServer();

        Hashtable GetServerMappingForConfig();

        void UpdateServerMappingConfig(MappingConfiguration.Dom.MappingConfiguration mappingConfiguration);

        MappingConfiguration.Dom.MappingConfiguration GetServerMappingForClient();

        void SetPublicIPConfiguration(string publicIp);

        void GarbageCollect(bool block, bool isCompactLOH);

        void ApplyHotConfiguration(string cacheId, HotConfig hotConfig);

        int GetProcessID(string cacheId);

        Alachisoft.NCache.Caching.Cache GetCache(string cacheId);

        void StopCacheInstance(string cache, CacheInfo cacheInfo, CacheServer.CacheStopReason reason);

        void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, String cacheId, byte[] transferCommand);

        string GetCacheName(int port);
        
        void StopCacheOnCacheHost(string cacheName);

        void StopCachesOnNode(ArrayList cacheName);
       
        Hashtable GetAPILogData(string cacheID, string instanceID);

        void SavePerfmonLogFilePath(string path);

        bool GetPerfmonConfiguration();

        string GetPerfmonLoggingPath();
        
        ConfigurationVersion GetConfigurationVersion(string cacheId);
        int GetCacheProcessID(string cacheID);
        int GetServiceProcessID(string serviceName);
        Dictionary<string, TopicStats> GetTopicStats(string cacheId,bool defaultTopicStats = false);

        // double GetCounterValue(string cacheId, string counterName);
        
        double GetCounterValue(string cacheId, string counterName, string category,bool replica = false);

        void PublishCustomClientCounters(string cacheId, ClientCustomCounters customCounters);

        void LogBackingSourceStatus(string cacheId);

        bool AreCacheHostCountersEnabled();


        Dictionary<string, Config.NewDom.CacheServerConfig> GetConfigurationOfAllCaches(CacheTopology topology);

        string GetConfigurationId(string cacheName);
        void ChannelDisconnected(IChannelDisconnected channelDisconnection);

        void StartNCacheWebManagementProcess(byte[] userId, byte[] password);

        bool StopNCacheWebManagementProcess(byte[] userId, byte[] password);

        PoolStats GetPoolStats(PoolStatsRequest request);

        ServerLicenseInfo GetCachedServerLicenseInfo();
        bool IsUnderStateTransfer(string cacheId);
       
        MemoryDumpMetainfo TakeMemoryDump(int processId, String CacheName, bool waitForCompletion = false);
        bool isDumpCompleted(int dumpId);
        FileMetaInfo[] GetMemoryDumpList();
        bool RemoveMemoryDump(String fileName);
        FileMetaInfo[] GetCacheLogList(string cacheId);
        IDictionary GetCacheLogs(IList<string> fileNames);
        Config.NewDom.CacheServerConfig[] GetCacheServerConfiguration();
        string GetMachineId();
        string GetPossibleMachinesInCluster();
        ClientProfileDom GetClientProfileReport(string cacheId);
        OSInfo GetOSPlatform();
        void StartStressTest(string cacheName, int executionTime);
        /// <summary>
        /// Return module specific number of buckets for managing distribution
        /// </summary>
        int GetModuleBucketsCount();

        /// <summary>
        /// Verifies whether a given directory is valid or not
        /// </summary>
        /// <param name="Path">path of the directory</param> 
        bool ValidateDirectory(string path, bool isLocalExisting);
        DistributionInfo GetDistributionInfo(string cacheId, string existedMapPath);

        void RollBackMapPropogation(string mapId);
        void InstallDistributionMap(DistributionInfo distributionInfo);


        bool DeleteBucket(string path);

        object PerformCloudServiceRelatedTasks(CloudServiceParams cloudServiceParams);
        CumulativeCounters GetCumulativeCountersForAllRegisteredCaches();

        InstallationTypeProvider GetInstallationTypeProvider();

        /// <summary>
        /// Retrieve file names from specified path
        /// </summary>
        /// <param name="path">path from where file names need to be retrieved</param>
        /// 
        string[] GetFullFileNamesFromPath(string path);

        /// <summary>
        /// Retrieve folder names from specified path
        /// </summary>
        /// <param name="path">path from where folder names need to be retrieved</param>
        /// 
        string[] GetDirectoryNamesFromPath(string path);

        /// <summary>
        /// reads file content to byte array
        /// </summary>
        /// <param name="path">full file path</param>
        /// 
        byte[] ReadFileContentToByteArray(string path);

        CacheConfigInfo GetCacheConfigInfo(string cacheId);


        /// <summary>
        /// To add or update key-value pairs in the service.exe.config file of NCache.
        /// </summary>
        /// <param name="key">Key name of the paramter that needs to be added or updated</param>
        /// <param name="value">Value of the specified key paramter</param>
        bool ApplyServiceConfig(string key, string value);


    }
}
