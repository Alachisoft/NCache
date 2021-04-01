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
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ServiceGetOptimalServerCommand : CommandBase
    {
        private long acknowledgementId;

        public ServiceGetOptimalServerCommand(long acknowledgementId)
        {
            // TODO: Complete member initialization
            this.acknowledgementId = acknowledgementId;
        }
        private struct CommandInfo
        {
            public string RequestId;
            public string CacheId;
            public string UserName;
            public string Password;
            public byte[] UserNameBinary;
            public byte[] PasswordBinary;
            public bool IsDotNetClient;

           
        }


        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo;
            try
            {
                cmdInfo = ParseCommand(command, clientManager);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2")) 
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }
            try
            {
                SocketHelper.TransferConnection(clientManager, cmdInfo.CacheId, command, this.acknowledgementId);
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetOptimalServerCommand getOptimalServerCommand = command.getOptimalServerCommand;

            cmdInfo.CacheId = getOptimalServerCommand.cacheId;
            return cmdInfo;
        }

       
    }
}
