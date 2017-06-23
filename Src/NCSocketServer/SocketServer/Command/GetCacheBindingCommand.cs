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
            public bool IsJavaClient;

            public CommandInfo clone()
            {
                CommandInfo varCopy = new CommandInfo();

                varCopy.RequestId = this.RequestId;
                varCopy.CacheId = this.CacheId;
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
                    _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
                }
                return;
            }
            try
            {

                int port = 0;

                port = ServiceConfiguration.Port;
                
                bool isRunning = CacheProvider.Provider.IsRunning(cmdInfo.CacheId);
                string bindIP = ServiceConfiguration.BindToIP.ToString();

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                GetCacheBindingResponse getCacheBindingResponse = new GetCacheBindingResponse();
                getCacheBindingResponse.isRunning = isRunning;
                getCacheBindingResponse.port = port;
                getCacheBindingResponse.server = bindIP;
                response.requestId = Convert.ToInt64(cmdInfo.RequestId);
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_CACHE_BINDING;
                response.getCacheBindingResponse = getCacheBindingResponse;
                
                _serializedResponsePackets.Add(ResponseHelper.SerializeResponse(response));

            }
            catch (System.Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponse(ex, command.requestID));
            }
        }

        private GetCacheBindingCommand.CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.GetCacheBindingCommand getCacheBindingCommand = command.getCacheBindingCommand;

            cmdInfo.CacheId = getCacheBindingCommand.cacheId;
            cmdInfo.IsJavaClient = getCacheBindingCommand.isJavaClient;
            cmdInfo.RequestId = command.requestID.ToString();

            return cmdInfo;
        }
    }
}
