// Copyright (c) 2017 Alachisoft
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
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.IO;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ServiceInitializeCommand : CommandBase
    {
        CommandInfo cmdInfo;
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public bool IsDotNetClient;
            public string ClientID;
            public int clientVersion;
            public string clientIP;
            public bool isAzureClient;
        }
        
        private long _acknowledgmentId;
        //PROTOBUF

        public ServiceInitializeCommand(long acknowledgementId)
        {
            _acknowledgmentId = acknowledgementId;
        }

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " parsing error " + exc.ToString());

                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }

            try
            {
                clientManager.ClientVersion = cmdInfo.clientVersion;
                SocketHelper.TransferConnection(clientManager, cmdInfo.CacheId, command, _acknowledgmentId);
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + exc.ToString());
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }
       
        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
            cmdInfo.CacheId = initCommand.cacheId;
            cmdInfo.clientVersion = initCommand.clientVersion;
            return cmdInfo;
        }

    }
}
