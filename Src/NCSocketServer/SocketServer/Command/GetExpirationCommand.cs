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
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching;
namespace Alachisoft.NCache.SocketServer.Command
{
    class GetExpirationCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string requestId;
           
        }
       public override void  ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
       {
           CommandInfo commandInfo;
           try
           {
               commandInfo = ParseCommand(command);
           }
           catch (Exception exc)
           {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
           }

           NCache nCache = clientManager.CmdExecuter as NCache;
           Caching.Cache cache = nCache.Cache;
    
           Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
           response.requestId=Convert.ToInt64(commandInfo.requestId);
           response.commandID=command.commandID;
           response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.EXPIRATION_RESPONSE;

            //PROTOBUF:RESPONSE
            if (clientManager.ClientVersion >= 5000)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response, Common.Protobuf.Response.Type.EXPIRATION_RESPONSE));
            }
            else
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }

       }

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetExpirationCommand getExpirationCommand = command.getExpirationCommand;
            cmdInfo.requestId = getExpirationCommand.requestId.ToString();
            return cmdInfo;
        }
    }
}
