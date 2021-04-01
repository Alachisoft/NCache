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
//using System.Linq;
using System.Text;
using Alachisoft.NCache.SocketServer.Command;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Config.Dom;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.SocketServer.Util;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class ServiceCacheBindingCommand : CommandBase
    {

        private long acknowledgementId;
        private struct CommandInfo 
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsJavaClient;

        }

        public ServiceCacheBindingCommand(long acknowledgementId)
        {
            this.acknowledgementId = acknowledgementId;
        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (System.Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                }
                return;
            }
            try
            {
                SocketHelper.TransferConnection(clientManager, cmdInfo.CacheId, command, acknowledgementId);

            }
            catch (System.Exception ex)
            {
                _serializedResponsePackets.Add(ResponseHelper.SerializeExceptionResponseWithType(ex, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();
            Alachisoft.NCache.Common.Protobuf.GetCacheBindingCommand getCacheBindingCommand = command.getCacheBindingCommand;
            cmdInfo.CacheId = getCacheBindingCommand.cacheId;
            return cmdInfo;
        }
    }
}
