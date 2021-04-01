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
using System.Text;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Protobuf;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetCacheBindingCommand : CommandBase
    {

        private struct CommandInfo
        {

            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsJavaClient;
            public CommandInfo clone() 
            {
                CommandInfo varCopy = new CommandInfo();

                varCopy.RequestId = this.RequestId;
                varCopy.CacheId = this.CacheId;
                varCopy.UserName = this.UserName;
                varCopy.Password = this.Password;
                varCopy.UserNameBinary = this.UserNameBinary;
                varCopy.PasswordBinary = this.PasswordBinary;
                varCopy.IsJavaClient = this.IsJavaClient;

                return varCopy;
            }
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            try
            {
                cmdInfo = ParseCommand(command, clientManager).clone();
            }
            catch (System.Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
                }
                return;
            }
            try

            {
                int port = ServiceConfiguration.Port; 
                string bindIP = ServiceConfiguration.BindToIP.ToString();
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                GetCacheBindingResponse getCacheBindingResponse = new GetCacheBindingResponse();
                getCacheBindingResponse.isRunning = true;
                getCacheBindingResponse.port = port;
                getCacheBindingResponse.server = bindIP;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_CACHE_BINDING;
                response.getCacheBindingResponse = getCacheBindingResponse;
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(response));

            }
            catch (System.Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponseWithoutType(ex, command.requestID, command.commandID));
            }
        }

        private GetCacheBindingCommand.CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.GetCacheBindingCommand getCacheBindingCommand = command.getCacheBindingCommand;

            cmdInfo.CacheId = getCacheBindingCommand.cacheId;
            cmdInfo.IsJavaClient = getCacheBindingCommand.isJavaClient;
            cmdInfo.RequestId = command.requestID.ToString();
            cmdInfo.Password = getCacheBindingCommand.pwd;
            cmdInfo.UserName = getCacheBindingCommand.userId;

            cmdInfo.PasswordBinary = getCacheBindingCommand.binaryPassword;
            cmdInfo.UserNameBinary = getCacheBindingCommand.binaryUserId;

            return cmdInfo;
        }
    }
}
