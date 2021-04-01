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
using System.Net;
using Alachisoft.NCache.Runtime.Exceptions;
using Alachisoft.NCache.SocketServer.Util;
using Alachisoft.NCache.Caching.Statistics;
using Alachisoft.NCache.Common.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Logger;
using System.Collections;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using System.IO;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ServiceInitializeCommand : CommandBase
    {
        CommandInfo cmdInfo;
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
           
            public byte[] UserNameBinary;
            public byte[] PassworNameBinary;
            public string clientIP;
            public bool isAzureClient;
            public int CommandVersion;
            public string clientEditionId;
        
        }
        private bool requestLoggingEnabled;

        private long _acknowledgmentId;
        //PROTOBUF

        public ServiceInitializeCommand(bool requestLoggingEnabled, long acknowledgementId)
        {
            this.requestLoggingEnabled = requestLoggingEnabled;
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

              //  if (!base.immatureId.Equals("-2") )
                {
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }

            try
            {
                SocketHelper.TransferConnection(clientManager, cmdInfo.CacheId, command, _acknowledgmentId);
            }

            catch (SecurityException sec)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + sec.ToString());
                _serializedResponsePackets.Add(GetOptimizedResponse(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(sec, command.requestID, command.commandID, clientManager.ClientVersion)));
            }

            catch (Exception exc)
            {
                if (SocketServer.Logger.IsErrorLogsEnabled) SocketServer.Logger.NCacheLog.Error("InitializeCommand.Execute", clientManager.ClientSocket.RemoteEndPoint.ToString() + " : " + clientManager.ClientID + " failed to connect to " + cmdInfo.CacheId + " Error: " + exc.ToString());
                _serializedResponsePackets.Add(GetOptimizedResponse(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion)));
            }
        }
       
        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.InitCommand initCommand = command.initCommand;
            cmdInfo.CacheId = initCommand.cacheId;
            cmdInfo.clientVersion = initCommand.clientVersion;
            cmdInfo.clientEditionId = initCommand.clientEditionId;
            return cmdInfo;
        }


        /// <summary>
        /// Send Service Initialize Command respose on optimal path if exception occured only for newer clients 4610.
        /// Backward compability handled for older version client. 
        /// </summary>
        /// <param name="unOptBuffer"></param>
        /// <returns></returns>
        private IList GetOptimizedResponse(IList unOptBuffer)
        {
            if (cmdInfo.clientVersion < 4610) return unOptBuffer;
            
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {

                byte[] dataSzBytes = new byte[ConnectionManager.MessageSizeHeader];
                stream.Write(dataSzBytes, 0, ConnectionManager.MessageSizeHeader);

                int len = 0;

                foreach (byte[] buffBytes in unOptBuffer)
                {
                    stream.Write(buffBytes, 0, buffBytes.Length);
                    len += buffBytes.Length;
                }

                byte[] lengthBytes = HelperFxn.ToBytes(len.ToString());

                stream.Seek(0, SeekOrigin.Begin);
                stream.Write(lengthBytes, 0, lengthBytes.Length);

                return stream.GetInternalBuffer();
            }
        }
       
    }
}
