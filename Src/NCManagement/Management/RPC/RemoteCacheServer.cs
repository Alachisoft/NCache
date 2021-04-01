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
using System.Collections.Generic;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Collections;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.RPCFramework;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Caching.Util;
using Alachisoft.NCache.Common.Topologies.Clustered;
using System.Net;
using Alachisoft.NCache.Management.Statistics;
using System.Linq;
using Alachisoft.NCache.Common.Communication;
using Alachisoft.NCache.Common.Pooling.Stats;
using Alachisoft.NCache.Common.FeatureUsageData.Dom;

namespace Alachisoft.NCache.Management.RPC
{
    public class RemoteCacheServer : RemoteServerBase, ICacheServer
    {
        private string _user;
        private string _password;
        private string _source;
        static RemoteCacheServer()
        {
            CacheServer.RegisterCompactTypes();
        }

        public string Source
        {
            get { return _source; }
            set { _source = value; }
        }

      
        public RemoteCacheServer(string server, int port) : base(server, port) { }

        public RemoteCacheServer(string server, int port, string bindIp) : base(server, port, bindIp) { }

        public RemoteCacheServer(string server, int port, string bindIp, string user, string password)
            : base(server, port, bindIp)
        {
            _user = user;
            _password = password;
        }

        protected override Common.Communication.IChannelFormatter GetChannelFormatter()
        {
            return new ManagementChannelFormatter();
        }

      
        protected object ExecuteCommandOnCacehServer(Alachisoft.NCache.Common.Protobuf.ManagementCommand command)
        {
            ManagementResponse response = null;
            if (_requestManager != null)
            {
                try
                {
                    response = _requestManager.SendRequest(command) as ManagementResponse;
                }
                catch (System.Exception e)
                {
                    throw new ManagementException(e.Message, e);
                }

                if (response != null && response.exception != null)
                {
                    if (response.exception.type == Alachisoft.NCache.Common.Protobuf.Exception.Type.SECURITY)
                        throw new SecurityException(response.exception.message);
                    if (response.exception.type == Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATON_EXCEPTION)
                        throw new VersionException (response.exception.message,response.exception.errorCode);
                  
                    throw new ManagementException(response.exception.message);
                }
            }

            if (response != null)
                return response.ReturnValue;

            return null;
        }

        private ManagementCommand GetManagementCommand(string method)
        {
            return GetManagementCommand(method, 1);
        }

        private ManagementCommand GetManagementCommand(string method, int overload)
        {
            ManagementCommand command = new ManagementCommand();
            command.methodName = method;
            command.overload = overload;
            command.objectName = ManagementUtil.ManagementObjectName.CacheServer;

            if (!string.IsNullOrEmpty(Source))
            {
                if (Source.ToLower() == "manager")
                    command.source = ManagementCommand.SourceType.MANAGER;
                else if (Source.ToLower() == "monitor")
                    command.source = ManagementCommand.SourceType.MONITOR;
                else
                    command.source = ManagementCommand.SourceType.TOOL;
            }
            else
                command.source = ManagementCommand.SourceType.TOOL;

            return command;
        }

        #region /               --- Cache Server Public Interface ---                       /

        public string GetClusterIP()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClusterIP);
            return ExecuteCommandOnCacehServer(command) as string;
        }

        public string GetLocalCacheIP()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetLocalCacheIP);
            return ExecuteCommandOnCacehServer(command) as string;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="buffer"></param>
        public void CopyAssemblies(string cacheName, string assemblyFileName, byte[] buffer)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.CopyAssemblies);

            command.Parameters.AddParameter(cacheName);
            command.Parameters.AddParameter(assemblyFileName);
            command.Parameters.AddParameter(buffer);

            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">Name of the file (assembly)</param>
        /// <param name="buffer"></param>
        public void CopyAssemblies(string cacheName, Dictionary<string, byte[]> filesData)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.CopyAssemblies,2);

            command.Parameters.AddParameter(cacheName);
            command.Parameters.AddParameter(filesData);

            ExecuteCommandOnCacehServer(command);
        }
        
        public Node[] GetCacheServers(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheServers);

            command.Parameters.AddParameter(cacheId);

            return (Node[])ExecuteCommandOnCacehServer(command);

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="cacheName"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public byte[] GetAssembly(string cacheName, string fileName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetAssembly);

            command.Parameters.AddParameter(cacheName);
            command.Parameters.AddParameter(fileName);

            return ExecuteCommandOnCacehServer(command) as byte[];
        }


        /// <summary>
        /// Clear cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <summary>
        public void ClearCache(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ClearCache);

            command.Parameters.AddParameter(cacheId);
            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Get a list of running caches (local + clustered)
        /// </summary>
        /// <returns>list of running caches</returns>        
        public ArrayList GetRunningCaches()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetRunningCaches, 2);
            return ExecuteCommandOnCacehServer(command) as ArrayList;
        }



        public IDictionary GetCacheProps()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheProps);


            return ExecuteCommandOnCacehServer(command) as IDictionary;
        }

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
        public IDictionary CacheProps()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.CacheProps);

            return ExecuteCommandOnCacehServer(command) as IDictionary;
        }

        public CacheServerConfig GetCacheConfiguration(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheConfiguration);
            command.Parameters.AddParameter(cacheId);

            return ExecuteCommandOnCacehServer(command) as CacheServerConfig;
        }

        public CacheInfo GetCacheInfo(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheInfo);
            command.Parameters.AddParameter(cacheId);

            return ExecuteCommandOnCacehServer(command) as CacheInfo;
        }

        public string GetHostName()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetHostName);

            return ExecuteCommandOnCacehServer(command) as string;
        }

        public void ReloadSrvcConfig()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ReloadSrvcConfig);

            ExecuteCommandOnCacehServer(command);
        }

        public int GetSocketServerPort()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetSocketServerPort);

            return (int)ExecuteCommandOnCacehServer(command);
        }

        public CacheRegisterationInfo GetUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetUpdatedCacheConfiguration);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partId);
            command.Parameters.AddParameter(newNode);
            command.Parameters.AddParameter(isJoining);

            return ExecuteCommandOnCacehServer(command) as CacheRegisterationInfo;
        }

        /// <summary>
        /// Register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <param name="overwrite"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        /// 

        public bool RegisterCache(string cacheId, Alachisoft.NCache.Config.NewDom.CacheServerConfig config, string partId, bool overwrite, bool hotApply)
        {
            //overload 2
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.RegisterCache, 2);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(config);
            command.Parameters.AddParameter(partId);
            command.Parameters.AddParameter(overwrite);
            command.Parameters.AddParameter(hotApply);
            return (bool)ExecuteCommandOnCacehServer(command);

        }

        /// <summary>
        /// Adds Server Node
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="config"></param>
        /// <param name="partId"></param>
        /// <param name="overwrite"></param>
        /// <param name="userId"></param>
        /// <param name="password"></param>
        /// <param name="hotApply"></param>
        /// <returns></returns>
        public bool RegisterCache(string cacheId, CacheServerConfig config, string partId, bool overwrite, bool hotApply)
        {
            //overload 1
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.RegisterCache, 1);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(config);
            command.Parameters.AddParameter(partId);
            command.Parameters.AddParameter(overwrite);
            command.Parameters.AddParameter(hotApply);
            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public NodeInfoMap GetNodeInfo()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetNodeInfo);
            return ExecuteCommandOnCacehServer(command) as NodeInfoMap;
        }

        public string CanApplyHotConfiguration(string cacheId, CacheServerConfig config)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.CanApplyHotConfig);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(config);
            return ExecuteCommandOnCacehServer(command) as string;
        }

        /// <summary>
        /// Gets or sets the socket server port.
        /// </summary>



        public void RemoveCacheServerFromClientConfig(string cacheId, string serverName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.RemoveCacheServerFromClientConfig);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(serverName);

            ExecuteCommandOnCacehServer(command);
        }

        public void RemoveCacheFromClientConfig(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.RemoveCacheFromClientConfig);
            command.Parameters.AddParameter(cacheId);

            ExecuteCommandOnCacehServer(command);
        }

        public void UpdateClientServersList(string cacheId, Alachisoft.NCache.Management.ClientConfiguration.CacheServerList serversPriorityList, string serverRuntimeContext)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.UpdateClientServersList);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(serversPriorityList);
            command.Parameters.AddParameter(serverRuntimeContext);
            ExecuteCommandOnCacehServer(command);
        }

        public void UpdateClientServersList(string cacheId, string[] servers, ref string xml, string clientCacheId, bool loadBalance)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.UpdateClientServersList, 2);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(servers);
            command.Parameters.AddParameter(xml);
            command.Parameters.AddParameter(clientCacheId);
            command.Parameters.AddParameter(loadBalance);
            
            ExecuteCommandOnCacehServer(command);
        }

        public ClientConfiguration.Dom.ClientConfiguration GetClientConfiguration(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClientConfiguration);
            command.Parameters.AddParameter(cacheId);

            return ExecuteCommandOnCacehServer(command) as ClientConfiguration.Dom.ClientConfiguration;
        }

        public void UpdateClientConfiguration(string cacheId, ClientConfiguration.Dom.ClientConfiguration configuration)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.UpdateClientConfiguration);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(configuration);

            ExecuteCommandOnCacehServer(command);
        }

        public string GetBindIP()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetBindIP);

            return ExecuteCommandOnCacehServer(command) as string;
        }

        public int GetClientConfigId()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClientConfigId);

            return (int)ExecuteCommandOnCacehServer(command);
        }

        public ClientNodeStatusWrapper GetClientNodeStatus(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClientNodeStatus);
            command.Parameters.AddParameter(cacheId);

            return (ClientNodeStatusWrapper)ExecuteCommandOnCacehServer(command);
        }


        public bool VerifyWindowsUser(string nodeName, string userName, string password)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.VerifyWindowsUser);
            command.Parameters.AddParameter(nodeName);
            command.Parameters.AddParameter(userName);
            command.Parameters.AddParameter(password);

            return (bool)ExecuteCommandOnCacehServer(command);
        }


        //[salman]
        //Method introduced to check weather a user is a windows administrator or not
        public bool VerfyAdministrator(string userName, string password)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.VerfyAdministrator);
            command.Parameters.AddParameter(userName);
            command.Parameters.AddParameter(password);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public bool VerifyNodeAdministrator(string userName, string password)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.VerifyNodeAdministrator);
            command.Parameters.AddParameter(userName);
            command.Parameters.AddParameter(password);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Disbale logging
        /// </summary>
        /// <param name="subsystem">Subsystem for which logging will be disabled</param>
        /// <param name="type">Type of logging to disable</param>
        public void DisableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.DisableLogging);
            command.Parameters.AddParameter(subsystem);
            command.Parameters.AddParameter(type);

            ExecuteCommandOnCacehServer(command);
        }

        public void EnableLogging(LoggingInfo.LoggingSubsystem subsystem, LoggingInfo.LoggingType type)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.EnableLogging);
            command.Parameters.AddParameter(subsystem);
            command.Parameters.AddParameter(type);

            ExecuteCommandOnCacehServer(command);
        }

        public void SynchronizeClientConfig()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.SynchronizeClientConfig);

            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Update TCP cache settings that includes updated list of TCP members
        /// </summary>
        /// <param name="cacheId"></param>
        /// <param name="props"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        public bool ApplyCacheConfiguration(string cacheId, CacheServerConfig props, bool hotApply)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ApplyCacheConfiguration);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(props);

            command.Parameters.AddParameter(hotApply);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Update TCP cache settings that includes updated list of TCP members
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        public void ApplyHotConfiguration(string cacheId, HotConfig hotConfig)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.HotApplyConfiguration);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(hotConfig);
            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Un-register cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        public void UnregisterCache(string cacheId, string partId, bool removeServerOnly)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.UnregisterCache);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partId);
            command.Parameters.AddParameter(removeServerOnly);

            ExecuteCommandOnCacehServer(command);
        }

        [TargetMethod(ManagementUtil.MethodName.StartCache, 1)]
        public void StartCache(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StartCache);
            command.Parameters.AddParameter(cacheId);
            ExecuteCommandOnCacehServer(command);
        }

        public void StartCache(string cacheId, string partitionId, bool twoPhaseInitialization)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, twoPhaseInitialization);
        }

        public void StartCachePhase2(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StartCachePhase2);
            command.Parameters.AddParameter(cacheId);
            ExecuteCommandOnCacehServer(command);
        }

        public void StartCache(string cacheId, string partitionId)
        {
            StartCache(cacheId, partitionId, null, null, null, null, null, null, false);
        }

        public void StartCache(string cacheId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate)
        {
            StartCache(cacheId, null, itemAdded, itemRemoved, itemUpdated, cacheCleared, customRemove, customUpdate, false);
        }

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
        public void StartCache(string cacheId, string partitionId,
            ItemAddedCallback itemAdded,
            ItemRemovedCallback itemRemoved,
            ItemUpdatedCallback itemUpdated,
            CacheClearedCallback cacheCleared,
            CustomRemoveCallback customRemove,
            CustomUpdateCallback customUpdate,
            bool twoPhaseInitialization)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StartCache, 7);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partitionId);

            command.Parameters.AddParameter(null);
            command.Parameters.AddParameter(null);
            command.Parameters.AddParameter(null);
            command.Parameters.AddParameter(null);
            command.Parameters.AddParameter(null);
            command.Parameters.AddParameter(null);

            command.Parameters.AddParameter(twoPhaseInitialization);

            ExecuteCommandOnCacehServer(command);
        }


        public void StopCache(string cacheId)
        {
            StopCache(cacheId, null);
        }

        public int GetShutdownTimeout()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetShutdownTimeout);

            return (int)ExecuteCommandOnCacehServer(command);
        }




        /// <summary>
        /// Stop a cache
        /// </summary>
        /// <param name="cacheId"></param>
        /// <exception cref="ArgumentNullException">cacheId is a null reference (Nothing in Visual Basic).</exception>
        public void StopCache(string cacheId, string partitionId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopCache, 2);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partitionId);

            ExecuteCommandOnCacehServer(command);
        }

        public void StopCacheOnCacheHost(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopCacheOnHost, 1);
            command.Parameters.AddParameter(cacheId);
            ExecuteCommandOnCacehServer(command);
        }
        public void StopCachesOnNode(ArrayList cacheName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopCachesOnNode);
            command.Parameters.AddParameter(cacheName);
            ExecuteCommandOnCacehServer(command);
        }


        /// <summary>
        /// Detect and return all the available NICs on this machine
        /// </summary>
        public Hashtable DetectNICs()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.DetectNICs);

            return ExecuteCommandOnCacehServer(command) as Hashtable;
        }

        public void BindToIP(BindedIpMap bindIPMap)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.BindToIP);
            command.Parameters.AddParameter(bindIPMap);

            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public BindedIpMap BindedIp()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.BindedIp);
            return ExecuteCommandOnCacehServer(command) as BindedIpMap;
        }


        /// <summary>
        /// Gets the Max port number, among all the ports of registered caches on this machine
        /// </summary>
        /// <returns>Max cluster port</returns>
        public int GetMaxPort()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetMaxPort);

            return (int)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Gets the Max Socket port number, among all the ports of registered caches on this machine
        /// </summary>
        /// <returns>Max socket port</returns>
        public int GetMaxSocketPort()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetMaxSocketPort);

            return (int)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Checks if the current cache is a Cluster cache or not, used in NCache UnReg cache tool as now UnReg is only applicable to cluster caches only
        /// </summary>
        /// <returns>true if Cluster Cache</returns>
        public CacheStatusOnServerContainer IsClusteredCache(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsClusteredCache);
            command.Parameters.AddParameter(cacheId);

            return (CacheStatusOnServerContainer)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Checks whether the specified port is available (non-conflicting) or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the port is available, otherwise 'flase'</returns>
        public bool IsPortAvailable(int port, string cacheName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsPortAvailable);
            command.Parameters.AddParameter(port);
            command.Parameters.AddParameter(cacheName);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Checks whether the newly added node arise port conflict or not
        /// </summary>
        /// <param name="port">Cluster port</param>
        /// <returns>'true' if the node is allowed, otherwise 'flase'</returns>
        //internal bool NodeIsAllowed(int port)
        public bool NodeIsAllowed(int port, string id)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.NodeIsAllowed);
            command.Parameters.AddParameter(port);
            command.Parameters.AddParameter(id);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Gets the status of NCache on this node.
        /// </summary>
        /// <returns>The ServerStatus.</returns>

        public StatusInfo GetCacheStatus(string cacheId, string partitionId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheStatus);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partitionId);

            return ExecuteCommandOnCacehServer(command) as StatusInfo;
        }


        /// <summary>
        /// Starts monitoring the client activity.
        /// </summary>
        public void StartMonitoringActivity()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StartMonitoringActivity);

            ExecuteCommandOnCacehServer(command);
        }
        /// <summary>
        /// Stops monitoring client activity.
        /// </summary>
        public void StopMonitoringActivity()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopMonitoringActivity);

            ExecuteCommandOnCacehServer(command);
        }

        /// <summary>
        /// Publishes the observed client activity into a file.
        /// </summary>
        public void PublishActivity()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.PublishActivity);

            ExecuteCommandOnCacehServer(command);
        }

        public void MakeCacheActive(string cacheId, bool active)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.MakeCacheActive);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(active);

            ExecuteCommandOnCacehServer(command);
        }
        

        public void ClearCacheContent(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ClearCacheContent);
            command.Parameters.AddParameter(cacheId);

            ExecuteCommandOnCacehServer(command);
        }

        public bool IsRunning(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsRunning);
            command.Parameters.AddParameter(cacheId);


            return (bool)ExecuteCommandOnCacehServer(command);
        }
        
        public bool IsUnderStateTransfer(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsUnderStateTransfer);
            command.Parameters.AddParameter(cacheId);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        

        public Caching.Statistics.CacheStatistics GetStatistics(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetStatistics);
            command.Parameters.AddParameter(cacheId);


            return ExecuteCommandOnCacehServer(command) as Alachisoft.NCache.Caching.Statistics.CacheStatistics;
        }

        public long GetCacheCount(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheCount);
            command.Parameters.AddParameter(cacheId);


            return (long)ExecuteCommandOnCacehServer(command);
        }

        public void SetLocalCacheIP(string ip)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.SetLocalCacheIP);
            command.Parameters.AddParameter(ip);

            ExecuteCommandOnCacehServer(command);
        }

        public void BalanceDataloadOnCache(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.BalanceDataloadOnCache);
            command.Parameters.AddParameter(cacheId);

            ExecuteCommandOnCacehServer(command);
        }

        public bool IsCacheRegistered(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsCacheRegistered);
            command.Parameters.AddParameter(cacheId);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public ConfiguredCacheInfo[] GetAllConfiguredCaches()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetAllConfiguredCaches);
            return (ConfiguredCacheInfo[])ExecuteCommandOnCacehServer(command);
        }

        public CacheNodeStatistics[] GetCacheStatistics(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheStatistics);
            command.Parameters.AddParameter(cacheId);
            return (CacheNodeStatistics[])ExecuteCommandOnCacehServer(command);
        }


        public Alachisoft.NCache.Caching.Statistics.CacheStatistics GetCacheStatistics2(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheStatistics2);
            command.Parameters.AddParameter(cacheId);
            return (Alachisoft.NCache.Caching.Statistics.CacheStatistics)ExecuteCommandOnCacehServer(command);
        }

        public string GetLicenseKey()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetLicenseKey);
            return (string)ExecuteCommandOnCacehServer(command);
        }

       
  

        public Hashtable GetSnmpPorts()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetSnmpPorts);
            return (Hashtable)ExecuteCommandOnCacehServer(command);
        }

        public void StopServer()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopServer);
            ExecuteCommandOnCacehServer(command);
        }

        public string GetServerPlatform()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetServerPlatform);
            return (string)ExecuteCommandOnCacehServer(command);
        }

        public ServerLicenseInfo GetServerLicenseInfo()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetServerLicenseInfo);
            return (ServerLicenseInfo)ExecuteCommandOnCacehServer(command);
        }

        public Config.NewDom.CacheServerConfig GetNewConfiguration(string cacheId)
        {

            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetNewConfiguration);
            command.Parameters.AddParameter(cacheId);

            return ExecuteCommandOnCacehServer(command) as Config.NewDom.CacheServerConfig;

        }

        public bool ApplyCacheConfiguration(string cacheId, Config.NewDom.CacheServerConfig props, bool hotApply)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ApplyCacheConfiguration, 2);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(props);

            command.Parameters.AddParameter(hotApply);

            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public NewCacheRegisterationInfo GetNewUpdatedCacheConfiguration(string cacheId, string partId, string newNode, bool isJoining)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetNewUpdatedCacheConfiguration);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(partId);
            command.Parameters.AddParameter(newNode);
            command.Parameters.AddParameter(isJoining);

            return ExecuteCommandOnCacehServer(command) as NewCacheRegisterationInfo;
        }

        public List<Common.Monitoring.ClientNode> GetCacheClients(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheClients);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as List<Common.Monitoring.ClientNode>;
        }

        public List<ClientProcessStats> GetClientProcessStats(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClientProcessStats);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as List<ClientProcessStats>;
        }

        public Common.ProductVersion GetProductVersion()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetProductVersion);
            return ExecuteCommandOnCacehServer(command) as Common.ProductVersion;
        }
        

        public void LogBackingSourceStatus(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.LogBackingSourceStatus);
            command.Parameters.AddParameter(cacheId);
            ExecuteCommandOnCacehServer(command);
        }

        public bool IsTayzGridServer()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetTayzGridServer);
            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public Hashtable GetServerMappingForConfig()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetServerMappingForConfig);
            return ExecuteCommandOnCacehServer(command) as Hashtable;
        }

        public MappingConfiguration.Dom.MappingConfiguration GetServerMappingForClient()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetServerMappingForClient);
            return ExecuteCommandOnCacehServer(command) as MappingConfiguration.Dom.MappingConfiguration;
        }

        public void UpdateServerMappingConfig(MappingConfiguration.Dom.MappingConfiguration mappingConfiguration)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.UpdateServerMappingConfig);
            command.Parameters.AddParameter(mappingConfiguration);
            ExecuteCommandOnCacehServer(command);
        }

        public void GarbageCollect(bool block, bool isCompactLOH)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GarbageCollect);
            command.Parameters.AddParameter(block);
            command.Parameters.AddParameter(isCompactLOH);
            ExecuteCommandOnCacehServer(command);
        }
        public int GetProcessID()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetProcessId);
            return (int)ExecuteCommandOnCacehServer(command);
        }
        public Alachisoft.NCache.Caching.Cache GetCache(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCache);
            command.Parameters.AddParameter(cacheId);
            return (Alachisoft.NCache.Caching.Cache)ExecuteCommandOnCacehServer(command);
        }

        public void StopCacheInstance(string cache, CacheInfo cacheInfo, CacheServer.CacheStopReason reason)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.StopCacheInstance);
            command.Parameters.AddParameter(cache);
            command.Parameters.AddParameter(cacheInfo);
            command.Parameters.AddParameter(reason);
            ExecuteCommandOnCacehServer(command);
        }

        public int GetProcessID(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetProcessId);
            command.Parameters.AddParameter(cacheId);
            return (int)ExecuteCommandOnCacehServer(command);
        }

        public bool PortIsAvailable(int port)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.IsPortAvailable);
            command.Parameters.AddParameter(port);
            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public void TransferConnection(System.Net.Sockets.SocketInformation socketInfo, String cacheId, byte[] transferCommand)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.TransferConnection);
            command.Parameters.AddParameter(socketInfo);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(transferCommand);
            ExecuteCommandOnCacehServer(command);
        }

        public string GetCacheName(int port)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheName);
            command.Parameters.AddParameter(port);
            return ((string)ExecuteCommandOnCacehServer(command)).ToLower();
        }

        public byte[] GetPerfmonLogFiles(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.HandleSet);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as byte[];
        }

        public Hashtable GetAPILogData(string cacheID, string instanceID)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetAPILogData, 1);
            command.Parameters.AddParameter(cacheID);
            command.Parameters.AddParameter(instanceID);
            return ExecuteCommandOnCacehServer(command) as Hashtable;
        }
        public void SavePerfmonLogFilePath(string path)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.SavePerfmonLogFilePath, 1);
            command.Parameters.AddParameter(path);
            ExecuteCommandOnCacehServer(command);
        }
        public bool GetPerfmonConfiguration()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonConfiguration, 1);
            return(bool) ExecuteCommandOnCacehServer(command);
        }

        public string GetPerfmonLoggingPath()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonLoggingPath, 1);
            return (string)ExecuteCommandOnCacehServer(command);
        }
       
        public ConfigurationVersion GetConfigurationVersion(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetConfigurationVersion, 1);
            command.Parameters.AddParameter(cacheId);
            return (ConfigurationVersion) ExecuteCommandOnCacehServer(command);
        }


        public Dictionary<string, TopicStats> GetTopicStats(string cacheId,bool defaultTopicStats= false)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetTopicStats);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(defaultTopicStats);
            return ExecuteCommandOnCacehServer(command) as Dictionary<string, TopicStats>;
        }

        public double GetCounterValue(string cacheId, string counterName, string category,bool replica = false)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCounterValue, 1);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(counterName);
            command.Parameters.AddParameter(category);
            command.Parameters.AddParameter(replica);
            return (double)ExecuteCommandOnCacehServer(command);
        }

        public int GetCacheProcessID(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetCacheProcessID, 1);
            command.Parameters.AddParameter(cacheId);
            return (int)ExecuteCommandOnCacehServer(command);
        }

        public void PublishCustomClientCounters(string cacheId, ClientCustomCounters customCounters)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.PublishCustomClientCounters);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(customCounters);
            ExecuteCommandOnCacehServer(command);
        }

        public string ActivateNCache(string key, string firstName, string lastName, string email, string company, string address, string state, string country, string phone, string zipCode, string city, string authCode, bool reactivation, bool manualActivation, string environment, int clients)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.ActivateNCache);
            command.Parameters.AddParameter(key);
            command.Parameters.AddParameter(firstName);
            command.Parameters.AddParameter(lastName);
            command.Parameters.AddParameter(email);
            command.Parameters.AddParameter(company);
            command.Parameters.AddParameter(address);
            command.Parameters.AddParameter(state);
            command.Parameters.AddParameter(country);
            command.Parameters.AddParameter(phone);
            command.Parameters.AddParameter(zipCode);
            command.Parameters.AddParameter(city);
            command.Parameters.AddParameter(authCode);
            command.Parameters.AddParameter(reactivation);
            command.Parameters.AddParameter(manualActivation);
            command.Parameters.AddParameter(environment);
            command.Parameters.AddParameter(clients);


            return (string)ExecuteCommandOnCacehServer(command);
        }

        public string DeactivateNCache(string key, bool manualDeactivation)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.DeactivateNCache);
            command.Parameters.AddParameter(key);
            command.Parameters.AddParameter(manualDeactivation);

            return (string)ExecuteCommandOnCacehServer(command);
        }

        public bool AreCacheHostCountersEnabled()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.AreCacheHostCountersEnabled, 1);
            return (bool)ExecuteCommandOnCacehServer(command);
        }

       

        public Dictionary<string, Config.NewDom.CacheServerConfig> GetConfigurationOfAllCaches(CacheTopology topology)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetConfigurationOfAllCaches);
            command.Parameters.AddParameter(topology);
            return ExecuteCommandOnCacehServer(command) as Dictionary<string, Config.NewDom.CacheServerConfig>;
        }


        public bool CanApplyHotConfiguration(string cacheId, long size)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.CanApplyHotConfig, 2);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(size);
            return (bool)ExecuteCommandOnCacehServer(command);
        }

        public string GetConfigurationId(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetConfigurationId);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as string;
        }
        
        public void ChannelDisconnected(IChannelDisconnected channelDisconnection)
        {
            OnChannelDisconnected(channelDisconnection);
        }

        public Dictionary<string,string> GetPerfmonCategoryNames()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonCategoryNames);
            return (Dictionary<string,string>)ExecuteCommandOnCacehServer(command);
        }

        public string[] GetPerfmonInstanceNames(string categoryName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonInstanceNames);
            command.Parameters.AddParameter(categoryName);
            return (string[])ExecuteCommandOnCacehServer(command);
        }

        public string[] GetPerfmonCounterNames(string categoryName, string instanceName)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonCounterNames);
            command.Parameters.AddParameter(categoryName);
            command.Parameters.AddParameter(instanceName);
            return (string[])ExecuteCommandOnCacehServer(command);
        }

        public double GetPerfmonValue(string cacheId, string counterName, string category, string processInstance, bool replica = false)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonValue, 1);
            command.Parameters.AddParameter(cacheId);
            command.Parameters.AddParameter(counterName);
            command.Parameters.AddParameter(category);
            command.Parameters.AddParameter(processInstance);
            command.Parameters.AddParameter(replica);
            return (double)ExecuteCommandOnCacehServer(command);
        }

        public List<PerfmonCounterDetails> GetPerfmonValues(List<PerfmonCounterDetails> counterDetails, string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPerfmonValues, 1);
            command.Parameters.AddParameter(counterDetails);
            command.Parameters.AddParameter(cacheId);
            return (List<PerfmonCounterDetails>)ExecuteCommandOnCacehServer(command);
        }

        public PoolStats GetPoolStats(PoolStatsRequest request)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetPoolStats, 1);
            command.Parameters.AddParameter(request);
            return ExecuteCommandOnCacehServer(command) as PoolStats;
        }


        public void AddConnectedClient(ClientProfile clientProfile)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.AddConnectedclient, 1);
            command.Parameters.AddParameter(clientProfile);

            ExecuteCommandOnCacehServer(command);
        }

        public void RemoveConnectedClient(string ipAddress)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.RemoveConnectedClient, 1);
            command.Parameters.AddParameter(ipAddress);
            ExecuteCommandOnCacehServer(command);
        }

        public Dictionary<string, Common.FeatureUsageData.Feature> GetFeatureUsageReport(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetFeatureUsageReport);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as Dictionary<string, Common.FeatureUsageData.Feature>;
        }

        public string GetMachineId()
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetMachineId);
            return ExecuteCommandOnCacehServer(command) as string;
        }

        public string GetPossibleMachinesInCluster()
        {
            return "";
        }

        public ClientProfileDom GetClientProfileReport(string cacheId)
        {
            ManagementCommand command = GetManagementCommand(ManagementUtil.MethodName.GetClientProfileReport);
            command.Parameters.AddParameter(cacheId);
            return ExecuteCommandOnCacehServer(command) as ClientProfileDom;
        }

        #endregion
        /// <summary>
        /// Finalizer for this object.
        /// </summary>
        /// 
        ~RemoteCacheServer()
        {
            Dispose();
        }

    }
}
       