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
    class GetExpirationCommand : CommandBase
    {
        protected struct CommandInfo
        {
            public string requestId;

        }

        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            CommandInfo commandInfo;
            try
            {
                commandInfo = ParseCommand(command);
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID, command.commandID));
                return;
            }
            NCache nCache = clientManager.CmdExecuter as NCache;
            Caching.Cache cache = nCache.Cache;
            Alachisoft.NCache.Config.Dom.ExpirationPolicy policy = null;
            policy = cache.Configuration.ExpirationPolicy;
            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            Alachisoft.NCache.Common.Protobuf.GetExpirationResponse getExpirationResponse = new Alachisoft.NCache.Common.Protobuf.GetExpirationResponse();
            getExpirationResponse.absDefault = policy.AbsoluteExpiration.Default;
            getExpirationResponse.absDefaultEnabled = policy.AbsoluteExpiration.DefaultEnabled;
            getExpirationResponse.sldDefault = policy.SlidingExpiration.Default;
            getExpirationResponse.sldDefaultEnabled = policy.SlidingExpiration.DefaultEnabled;
            getExpirationResponse.absLonger = policy.AbsoluteExpiration.Longer;
            getExpirationResponse.absLongerEnabled = policy.AbsoluteExpiration.LongerEnabled;
            getExpirationResponse.sldLonger = policy.SlidingExpiration.Longer;
            getExpirationResponse.sldLongerEnabled = policy.SlidingExpiration.LongerEnabled;
            response.getExpirationResponse = getExpirationResponse;
            response.requestId = Convert.ToInt64(commandInfo.requestId);
            response.commandID = command.commandID;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.EXPIRATION_RESPONSE;

            //PROTOBUF:RESPONSE

            _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
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
