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
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetOptimalServerCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsDotNetClient;

            public CommandInfo clone()
            {
                CommandInfo varCopy = new CommandInfo();

                varCopy.RequestId = this.RequestId;
                varCopy.CacheId = this.CacheId;
                varCopy.UserName = this.UserName;
                varCopy.Password = this.Password;
                varCopy.UserNameBinary = this.UserNameBinary;
                varCopy.PasswordBinary = this.PasswordBinary;
                varCopy.IsDotNetClient = this.IsDotNetClient;

                return varCopy;
            }
        }


        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            try
            {
                cmdInfo = ParseCommand(command, clientManager).clone();
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                return;
            }

            Alachisoft.NCache.Caching.Cache cache = null;

            try
            {
                string server = ConnectionManager.ServerIpAddress;
                int port = ConnectionManager.ServerPort;
                Hashtable serverPublicIp = null;
                cache = CacheProvider.Provider.GetCacheInstanceIgnoreReplica(cmdInfo.CacheId);
                if (cache == null) throw new Exception("Cache is not registered");
                if (!cache.IsRunning) throw new Exception("Cache is not running");


#if (SERVER ) 

               
                    if (cache.CacheType.Equals("replicated-server"))
                        cache.GetLeastLoadedServer(ref server, ref port, ref serverPublicIp);
                    else
                    {

                        if (cache.IsCoordinator) {  }
                        else
                            cache.GetActiveServer(ref server, ref port);

                        cache.GetServersPublicIPs(ref serverPublicIp);
                    }
#endif

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetOptimalServerResponse getOptimalServerResponse = new Alachisoft.NCache.Common.Protobuf.GetOptimalServerResponse();
                getOptimalServerResponse.server = server;
                getOptimalServerResponse.port = port;

                if (serverPublicIp != null && serverPublicIp.Count > 0)
                {
                    foreach (DictionaryEntry entry in serverPublicIp)
                    {
                        Common.Protobuf.KeyValuePair serverPublicip = new Common.Protobuf.KeyValuePair();
                        serverPublicip.key = entry.Key.ToString();
                        serverPublicip.value = entry.Value.ToString();
                        getOptimalServerResponse.serverPublicIp.Add(serverPublicip);
                    }
                }
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.getOptimalServer = getOptimalServerResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_OPTIMAL_SERVER;

                //PROTOBUF:RESPONSE

                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }

        //PROTOBUF
        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand getOptimalServerCommand = command.getOptimalServerCommand;

            cmdInfo.CacheId = getOptimalServerCommand.cacheId;
            cmdInfo.IsDotNetClient = getOptimalServerCommand.isDotnetClient;
            cmdInfo.Password = getOptimalServerCommand.pwd;
            cmdInfo.PasswordBinary = getOptimalServerCommand.binaryPassword;
            cmdInfo.RequestId = getOptimalServerCommand.requestId.ToString();
            cmdInfo.UserName = getOptimalServerCommand.userId;
            cmdInfo.UserNameBinary = getOptimalServerCommand.binaryUserId;

            return cmdInfo;
        }
    }
}
