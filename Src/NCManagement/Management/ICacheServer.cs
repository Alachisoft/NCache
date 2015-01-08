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
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Management.Management;

namespace Alachisoft.NCache.Management
{
    public interface ICacheServer : IDisposable
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

        bool Authorize();

        /// <summary>
        /// Get a list of running caches (local + clustered)
        /// </summary>
        /// <returns>list of running caches</returns>        
        ArrayList GetRunningCaches();
        
        IDictionary GetCacheProps();

        /// <summary>
        /// A collection of the cache infos registered with the server.
        /// </summary>
        /// <remarks>
        /// CacheProps are in new format now. Instead of saving the props string,
        /// it now saves CacheServerConfig instance:
        /// 
        /// |local-cache-id               | CacheServerConfig instance
        /// | | IDictionary
        ///                               | replica-id  | CacheServerConfig instance
        /// </remarks>
        IDictionary CacheProps();

        void BalanceDataloadOnCache(string cacheId);


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
        /// 

        bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply);

        
        /// <summary>
        /// Adds Server Node
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="config"></param>
        /// <param name="partId"></param>
        /// <param name="overwrite"></param>
        /// <param name="hotApply"></param>
        /// <returns></returns>
        bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply, bool isLocalNode);

        NodeInfoMap GetNodeInfo();

        string CanApplyHotConfiguration(string cacheId, CacheServerConfig config);

        void RemoveCacheServerFromClientConfig(string cacheId, string serverName);       

        void RemoveCacheFromClientConfig(string cacheId);

        void UpdateClientServersList(string cacheId, Alachisoft.NCache.Management.ClientConfiguration.CacheServerList serversPriorityList, string serverRuntimeContext);
        
        void UpdateClientServersList(string cacheId, string[] servers, ref string xml,  bool loadBalance);

        ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId);

        void UpdateClientConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration);

        string GetBindIP();


        ClientNodeStatusWrapper GetClientNodeStatus(string cacheId);

        int GetClientConfigId();
        Node[] GetCacheServers(string cacheId);

        ConfiguredCacheInfo[] GetAllConfiguredCaches();

       Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId);

        ConfiguredCacheInfo[] GetConfiguredPartitionedReplicaCaches();
      

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
        void UnregisterCache(string cacheId, string partId, bool removeServerOnly);        

        void StartCache(string cacheId);       

        void StartCache(string cacheId, string partitionId);        


        void StartCachePhase2(string cacheId);       
        /// <summary>
        /// Start a cache and provide call backs
        /// </summary>
        /// <param name="cahcheID"></param>
        /// <param name="propertyString"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        void StartCache(string cacheId, string partitionId,
        bool twoPhaseInitialization);
       

        void StopCache(string cacheId);
       

        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        void StopCache(string cacheId, string partitionId);

       


        /// <summary>
        /// Detect and return all the available NICs on this machine
        /// </summary>
        Hashtable DetectNICs();


        void BindToIP(BindedIpMap bindIPMap);
        
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        BindedIpMap BindedIp();


        /// <summary>
        /// Gets the Max port number, among all the ports of registered caches on this machine
        /// </summary>
        /// <returns>Max cluster port</returns>
        int GetMaxPort();
       

        /// <summary>
        /// Checks if the current cache is a Cluster cache or not, used in NCache UnReg cache tool as now UnReg is only applicable to cluster caches only
        /// </summary>
        /// <returns>true if Cluster Cache</returns>
        CacheStatusOnServerContainer IsClusteredCache(string cacheId);
       

        /// <summary>
        /// Checks whether the specified port is available (non-conflicting) or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the port is available, otherwise 'flase'</returns>
        bool PortIsAvailable(int port);
        

        /// <summary>
        /// Checks whether the newly added node arise port conflict or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the node is allowed, otherwise 'flase'</returns>
        //internal bool NodeIsAllowed(int port)
        bool NodeIsAllowed(int port, string id);

        /// <summary>
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>
        StatusInfo GetCacheStatus(string cacheId, string partitionId);
       


        /// <summary>
        /// Starts monitoring the client activity.
        /// </summary>
        void StartMonitoringActivity();
        
        /// <summary>
        /// Stops monitoring client activity.
        /// </summary>
        void StopMonitoringActivity();
        

        /// <summary>
        /// Publishes the observed client activity into a file.
        /// </summary>
        void PublishActivity();

       

        /// <summary>
        /// Clears the content of given cache
        /// </summary>
        /// <param name="cacheId">Id of the cache</param>
        void ClearCacheContent(string cacheId);

        /// <summary>
        /// Returns true if given cache is running
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        bool IsRunning(string cacheId);

        /// <summary>
        /// Gets CacheStatistics for a given cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        Alachisoft.NCache.Caching.Statistics.CacheStatistics GetStatistics(string cacheId);

        /// <summary>
        /// Get count for given cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <returns></returns>
        long GetCacheCount(string cacheId);



        bool IsCacheRegistered(string cacheId);

        void StopServer();

        string GetServerPlatform();

       
        
        Alachisoft.NCache.Config.NewDom.CacheServerConfig GetNewConfiguration(string cacheId);

        bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply);

        bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply, bool isLocalNode);

        bool ApplyCacheConfiguration(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig props, bool hotApply);

        NewCacheRegisterationInfo GetNewUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining);

        Common.ProductVersion GetProductVersion();  

       

        Hashtable GetServerMappingForConfig();

        void UpdateServerMappingConfig(MappingConfiguration.Dom.MappingConfiguration mappingConfiguration);

        MappingConfiguration.Dom.MappingConfiguration GetServerMappingForClient();
        void GarbageCollect(bool block, bool isCompactLOH);
    }
}
