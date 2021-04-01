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
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Management.ServiceControl;
using System.Threading;
using System.Net;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Common.Monitoring;
using Alachisoft.NCache.Common.FeatureUsageData;
using Alachisoft.NCache.Management;

namespace Alachisoft.NCache.SocketServer.Command
{
    class InitializeCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public bool IsDotNetClient;
            public string ClientID;
            public string LicenceCode;
            public int clientVersion;
            public Runtime.Caching.ClientInfo clientInfo;
            public byte[] UserNameBinary;
            public byte[] PassworNameBinary;
            public string clientIP;
            public bool isAzureClient;
            public int CommandVersion;
            public string clientEditionId;
            public bool SecureConnectionEnabled;
            public int operationTimeout;
            internal int cores;
        }
        private bool requestLoggingEnabled;
       
        //PROTOBUF

        public InitializeCommand(bool requestLoggingEnabled)
        {
            this.requestLoggingEnabled = requestLoggingEnabled;
        }
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " parsing error " + exc.ToString());

                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                }
                return;
            }

            try
            {
                clientManager.ClientID = cmdInfo.ClientID;
                clientManager.IsDotNetClient = cmdInfo.IsDotNetClient;
                clientManager.SupportAcknowledgement = cmdInfo.CommandVersion >= 2 && requestLoggingEnabled;
                clientManager.ClientVersion = cmdInfo.clientVersion;
                clientManager.SecureConnectionEnabled = cmdInfo.SecureConnectionEnabled;

                cmdInfo.clientInfo.ClientVersion = cmdInfo.clientVersion;

                GetClientUsage(clientManager, cmdInfo);

                ClientLedger.Instance.RegisterClientForCache(clientManager.ClientAddress, clientManager.ClientID);
                //Older client do not send operation timeout
                clientManager.RequestTimeout = cmdInfo.operationTimeout != -1? cmdInfo.operationTimeout: 90* 1000;

                clientManager.CmdExecuter = new NCache(cmdInfo.CacheId, cmdInfo.IsDotNetClient, clientManager, cmdInfo.LicenceCode, cmdInfo.UserName, cmdInfo.Password, cmdInfo.UserNameBinary, cmdInfo.PassworNameBinary, cmdInfo.clientInfo);

               
        
                if (clientManager.PoolManager == null)
                {
                    var shouldCreateFakePools = (clientManager.CmdExecuter as NCache).Cache.TransactionalPoolManager.IsUsingFakePools;
                    clientManager.ConnectionManager.CreatePools(shouldCreateFakePools);
                }

                clientManager.CacheTransactionalPool = ((NCache)clientManager.CmdExecuter).Cache.Context.TransactionalPoolManager;
                clientManager.CacheFakePool = ((NCache)clientManager.CmdExecuter).Cache.Context.FakeObjectPool;
                
             

                ClientManager cmgr = null;
                int noOfConnectedClients = 0;
                lock (ConnectionManager.ConnectionTable)
                {
                    if (ConnectionManager.ConnectionTable.Contains(clientManager.ClientID))
                    {
                        if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", "Another client with same clientID exists. Client ID is " + clientManager.ClientID);
                        cmgr = ConnectionManager.ConnectionTable[clientManager.ClientID] as ClientManager;

                        ConnectionManager.ConnectionTable.Remove(clientManager.ClientID);
                    }
                    ConnectionManager.ConnectionTable.Add(clientManager.ClientID, clientManager);
                    clientManager.ConnectionManager.PerfStatsColl.ConncetedClients = noOfConnectedClients = ConnectionManager.ConnectionTable.Count;
                }

                clientManager.CmdExecuter.OnClientConnected(clientManager.ClientID, cmdInfo.CacheId, cmdInfo.clientInfo, noOfConnectedClients);
                try
                {
                    if (cmgr != null)
                    {
                        cmgr.RaiseClientDisconnectEvent = false;
                        cmgr.Dispose();
                    }
                }
                catch (Exception e)
                {
                    if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", " an error occurred while forcefully disposing a client. " + e.ToString());
                }

                clientManager.EventQueue = new EventsQueue();
                clientManager.SlaveId = clientManager.ConnectionManager.EventsAndCallbackQueue.RegisterSlaveQueue(clientManager.EventQueue, clientManager.ClientID); // register queue with distributed queue.   
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientID + " is connected to " + cmdInfo.CacheId);

           
                //PROTOBUF:RESPONSE
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.InitializeCacheResponse initializeCacheResponse = new Alachisoft.NCache.Common.Protobuf.InitializeCacheResponse();
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.INIT;
                response.initCache = initializeCacheResponse;

                initializeCacheResponse.requestLoggingEnabled = this.requestLoggingEnabled;
                initializeCacheResponse.isPersistenceEnabled = ((NCache)clientManager.CmdExecuter).Cache.IsPersistEnabled;
                initializeCacheResponse.persistenceInterval = ((NCache)clientManager.CmdExecuter).Cache.PersistenceInterval;
                initializeCacheResponse.cacheType = ((NCache)clientManager.CmdExecuter).Cache.CacheType.ToLower();

#if SERVER
                initializeCacheResponse.targetCacheUniqueID = ((NCache)(clientManager.CmdExecuter)).Cache.TargetCacheUniqueID;
#endif
                //if graceful shutdown is happening on any server node...
                List<Alachisoft.NCache.Caching.ShutDownServerInfo> shutDownServers = ((NCache)(clientManager.CmdExecuter)).Cache.GetShutDownServers();

                if (shutDownServers != null && shutDownServers.Count > 0)
                {
                    initializeCacheResponse.isShutDownProcessEnabled = true;

                    foreach (Alachisoft.NCache.Caching.ShutDownServerInfo ssInfo in shutDownServers)
                    {
                        Alachisoft.NCache.Common.Protobuf.ShutDownServerInfo server = new Alachisoft.NCache.Common.Protobuf.ShutDownServerInfo();
                        server.serverIP = ssInfo.RenderedAddress.IpAddress.ToString();
                        server.port = ssInfo.RenderedAddress.Port;
                        server.uniqueKey = ssInfo.UniqueBlockingId;
                        server.timeoutInterval = ssInfo.BlockInterval;
                        initializeCacheResponse.shutDownServerInfo.Add(server);
                    }
                }


                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

                if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " connected to " + cmdInfo.CacheId);
            }

            catch (SecurityException sec)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + sec.ToString());
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(sec, command.requestID,command.commandID));
            }

            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + exc.ToString());
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }

        private bool IsServerAsClient(Node[] server, string clientIp)
        {
            if (server != null)
            {
                foreach (Node node in server)
                {
                    if (node.Address.IpAddress.ToString().Equals(clientIp))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;

            cmdInfo.CacheId = initCommand.cacheId;
            cmdInfo.ClientID = initCommand.clientId;
            cmdInfo.IsDotNetClient = initCommand.isDotnetClient;
            cmdInfo.LicenceCode = initCommand.licenceCode;
            cmdInfo.operationTimeout = initCommand.operationTimeout;
            

            cmdInfo.Password = initCommand.pwd;
            cmdInfo.PassworNameBinary = initCommand.binaryPwd;
            cmdInfo.RequestId = initCommand.requestId.ToString();
            cmdInfo.UserName = initCommand.userId;
            cmdInfo.UserNameBinary = initCommand.binaryUserid;
            cmdInfo.clientVersion = initCommand.clientVersion;
            cmdInfo.clientIP = initCommand.clientIP;
            cmdInfo.isAzureClient = initCommand.isAzureClient;
            clientManager.IsAzureClient = initCommand.isAzureClient;
            cmdInfo.clientEditionId = initCommand.clientEditionId;
            cmdInfo.SecureConnectionEnabled = initCommand.secureConnectionEnabled;
            cmdInfo.CommandVersion = command.commandVersion;
           
            if (cmdInfo.clientVersion < 4620)
            {
                cmdInfo.clientInfo = ClientInfo.TryParseLegacyClientID(initCommand.clientId);
                if (cmdInfo.clientInfo != null)
                {
                    cmdInfo.clientInfo.IPAddress = clientManager.ClientAddress;
                    cmdInfo.clientInfo.ClientID = "Legacy Client (AppName Not Supported)";

                }
            }
            else
            {
                cmdInfo.clientInfo = new Runtime.Caching.ClientInfo();
                cmdInfo.clientInfo.ProcessID = command.initCommand.clientInfo.processId;
                cmdInfo.clientInfo.AppName = command.initCommand.clientInfo.appName;
                cmdInfo.clientInfo.ClientID = command.initCommand.clientInfo.clientId;
                cmdInfo.clientInfo.MacAddress = command.initCommand.clientInfo.macAddress;
                cmdInfo.clientInfo.MachineName = command.initCommand.clientInfo.machineName;
                cmdInfo.clientInfo.IPAddress = clientManager.ClientAddress;
                cmdInfo.clientInfo.Cores = initCommand.clientInfo.cores;
                cmdInfo.clientInfo.Memory = command.initCommand.memory;
                cmdInfo.clientInfo.IsDotNetCore = command.initCommand.isDotNetCoreClient;
                cmdInfo.clientInfo.OperationSystem = command.initCommand.OperationSystem;
            }

            return cmdInfo;
        }

        private void GetClientUsage(ClientManager clientManager, CommandInfo cmdInfo)
        {
            ClientProfile clientProfile = new ClientProfile();

            if (clientManager.IsDotNetClient)
                clientProfile.Platform = ".net";
            else
                clientProfile.Platform = "java";

            if (cmdInfo.clientInfo.IsDotNetCore)
                clientProfile.Platform = ".netcore";

            clientProfile.EditionID = cmdInfo.clientEditionId;
            clientProfile.ClientId = cmdInfo.ClientID;
            clientProfile.Cores = cmdInfo.clientInfo.Cores;
            clientProfile.Mac = cmdInfo.clientInfo.MacAddress;
            clientProfile.IpAddress = cmdInfo.clientInfo.IPAddress.ToString();
            clientProfile.Version = ProductVersion.GetVersion();
            clientProfile.OperatingSystem = cmdInfo.clientInfo.OperationSystem;
            clientProfile.Memory = cmdInfo.clientInfo.Memory;
            clientManager.ClientProfile = clientProfile;


            if (cmdInfo.clientInfo != null && cmdInfo.clientInfo.AppName != null && cmdInfo.clientInfo.AppName.Contains(FeatureUsageCollector.FeatureTag))
            {
                FeatureUsageCollector.Instance.GetClientFeature(cmdInfo.clientInfo.AppName).UpdateUsageTime();
            }

        }
    }
}
