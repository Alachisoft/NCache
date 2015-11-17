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
//using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Util
{
    public class ManagementUtil
    {
        public struct MethodName
        {
            #region [ Cache Server]
            public const string StopServer = "StopServer";
            public const string StartCache = "StartCache";
            public const string GetClusterIP = "GetClusterIP";
            public const string GetLocalCacheIP = "GetLocalCacheIP";
            public const string CopyAssemblies = "CopyAssemblies";
           
            public const string GetAssembly = "GetAssembly";
            public const string ClearCache = "ClearCache";
            public const string Authorize = "Authorize";
            public const string GetRunningCaches = "GetRunningCaches";
            public const string GetCacheProps = "GetCacheProps";
            public const string CacheProps = "CacheProps";
            public const string GetCacheConfiguration = "GetCacheConfiguration";
            public const string GetCacheServers = "GetCacheServers";
            public const string GetCacheInfo = "GetCacheInfo";
            public const string GetHostName = "GetHostName";
            public const string GetAllConfiguredCaches = "GetAllConfiguredCaches";
            public const string GetCacheStatistics2 = "GetCacheStatistics2";
            public const string ReloadSrvcConfig = "ReloadSrvcConfig";
            public const string GetSocketServerPort = "GetSocketServerPort";
            public const string GetUpdatedCacheConfiguration = "GetUpdatedCacheConfiguration";
            public const string GetConfiguredPartitionedReplicaCaches = "GetConfiguredPartitionedReplicaCaches";
            public const string RegisterCache = "RegisterCache";
            public const string GetNodeInfo = "GetNodeInfo";
            public const string CanApplyHotConfig = "CanApplyHotConfig";
            public const string RemoveCacheServerFromClientConfig = "RemoveCacheServerFromClientConfig";
            public const string RemoveCacheFromClientConfig = "RemoveCacheFromClientConfig";
            public const string UpdateClientServersList = "UpdateClientServersList";
            public const string GetClientConfiguration = "GetClientConfiguration";
            public const string UpdateClientConfiguration = "UpdateClientConfiguration";
            public const string GetBindIP = "GetBindIP";
            public const string GetClientConfigId = "GetClientConfigId";
            public const string GetClientNodeStatus = "GetClientNodeStatus";
            public const string DisableLogging = "DisableLogging";
            public const string SynchronizeClientConfig = "SynchronizeClientConfig";
            public const string ApplyCacheConfiguration = "ApplyCacheConfiguration";
            public const string UnregisterCache = "UnregisterCache";
            public const string StartCachePhase2 = "StartCachePhase2";
            public const string StopCache = "StopCache";
            public const string DetectNICs = "DetectNICs";
            public const string BindToIP = "BindToIP";
            public const string BindedIp = "BindedIp";
            public const string GetMaxPort = "GetMaxPort";
            public const string IsClusteredCache = "IsClusteredCache";
            public const string PortIsAvailable = "PortIsAvailable";
            public const string NodeIsAllowed = "NodeIsAllowed";
            public const string GetCacheStatus = "GetCacheStatus";
            public const string StartMonitoringActivity = "StartMonitoringActivity";
            public const string StopMonitoringActivity = "StopMonitoringActivity";
            public const string PublishActivity = "PublishActivity";
            public const string EnableLogging = "EnableLogging";
            public const string GetCacheInstanceIgnoreReplica = "GetCacheInstanceIgnoreReplica";
            public const string GetCacheInstance = "GetCacheInstance";
            public const string MakeCacheActive = "MakeCacheActive";
            public const string ValidateProfessional = "ValidateProfessional";
            public const string ClearCacheContent = "ClearCacheContent";
            public const string IsRunning = "IsRunning";
            public const string GetStatistics = "GetStatistics";
            public const string GetCacheCount = "GetCacheCount";
            public const string SetLocalCacheIP = "SetLocalCacheIP";
            public const string BalanceDataloadOnCache = "BalanceDataloadOnCache";
            public const string IsCacheRegistered = "IsCacheRegistered";
            public const string GetServerPlatform = "GetServerPlatform";
            public const string GetNewConfiguration = "GetNewConfiguration";
            public const string GetNewUpdatedCacheConfiguration = "GetNewUpdatedCacheConfiguration";
            public const string GetProductVersion = "GetProductVersion";
            public const string GetServerMappingForConfig = "GetServerMappingForConfig";
            public const string UpdateServerMappingConfig = "UpdateServerMappingConfig";
            public const string GetServerMappingForClient = "GetServerMappingForClient";
            public const string GetUniqueServers = "GetUniqueServers";
            public const string GarbageCollect = "GarbageCollect";
            #endregion

        }

        public struct ManagementObjectName
        {
            public const string CacheServer = "CacheServer";
        }
    }
}
