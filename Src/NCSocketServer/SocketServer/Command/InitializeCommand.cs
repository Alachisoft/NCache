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


namespace Alachisoft.NCache.SocketServer.Command
{
    class InitializeCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public bool IsDotNetClient;
            public string ClientID;
            public string LicenceCode;
            public int clientVersion;
            public string clientIP;
            public bool isAzureClient;
        }

        //PROTOBUF

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;

            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Runtime.Exceptions.OperationNotSupportedException ex)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " " + ex.ToString());
             
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(ex, command.requestID));                
                return;
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " parsing error " + exc.ToString());

                if (!base.immatureId.Equals("-2"))
                {
                    //PROTOBUF:RESPONSE
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }
            

            try
            {
                clientManager.ClientID = cmdInfo.ClientID;
                clientManager.IsDotNetClient = cmdInfo.IsDotNetClient;
                clientManager.ClientVersion = cmdInfo.clientVersion;


                clientManager.CmdExecuter = new NCache(cmdInfo.CacheId, cmdInfo.IsDotNetClient, clientManager, cmdInfo.LicenceCode);

                ClientManager cmgr = null;
                lock (ConnectionManager.ConnectionTable)
                {
                    if (ConnectionManager.ConnectionTable.Contains(clientManager.ClientID))
                    {
                        if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", "Another client with same clientID exists. Client ID is " + clientManager.ClientID);
                        cmgr = ConnectionManager.ConnectionTable[clientManager.ClientID] as ClientManager;

                        ConnectionManager.ConnectionTable.Remove(clientManager.ClientID);
                    }
                    ConnectionManager.ConnectionTable.Add(clientManager.ClientID, clientManager);                 
                }

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
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.INIT;
                response.initCache = initializeCacheResponse;

                initializeCacheResponse.cacheType = ((NCache)clientManager.CmdExecuter).Cache.CacheType.ToLower();

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

                if (SocketServer.Logger.IsDetailedLogsEnabled) SocketServer.Logger.NCacheLog.Info("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " connected to " + cmdInfo.CacheId);
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + exc.ToString());
                //PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }
      
        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

           Common.Protobuf.InitCommand initCommand = command.initCommand;
            
            cmdInfo.CacheId = initCommand.cacheId;
            cmdInfo.ClientID = initCommand.clientId;
          
            cmdInfo.IsDotNetClient = initCommand.isDotnetClient;
            cmdInfo.LicenceCode = initCommand.licenceCode;

            cmdInfo.RequestId = initCommand.requestId.ToString();
            cmdInfo.clientVersion = initCommand.clientVersion;
            cmdInfo.clientIP = initCommand.clientIP;
            cmdInfo.isAzureClient = initCommand.isAzureClient;
            clientManager.ClientIP = initCommand.clientIP;
            clientManager.IsAzureClient = initCommand.isAzureClient;

            return cmdInfo;
        }
    }
}
