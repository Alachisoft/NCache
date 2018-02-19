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
using System.Collections;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal sealed class GetEnumeratorCommand : CommandBase
    {
        private struct CommandInfo
        {
            public string RequestId;
        }

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
				{
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
				}
                return;
            }

            int count = 0;
            string keyPackage = null;

            try
            {
                NCache nCache = clientManager.CmdExecuter as NCache;
				IDictionaryEnumerator dicEnu = (IDictionaryEnumerator)nCache.Cache.GetEnumerator();

				//PROTOBUF:RESPONSE
				Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
				Alachisoft.NCache.Common.Protobuf.GetEnumeratorResponse getEnumeratorResponse = new Alachisoft.NCache.Common.Protobuf.GetEnumeratorResponse();
				response.requestId = Convert.ToInt64(cmdInfo.RequestId);
				response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_ENUMERATOR;
				response.getEnum = getEnumeratorResponse;

				Alachisoft.NCache.SocketServer.Util.KeyPackageBuilder.PackageKeys(dicEnu, getEnumeratorResponse.keys);

				_serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));

            }
            catch (Exception exc)
            {
				//PROTOBUF:RESPONSE
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponse(exc, command.requestID));
            }
        }
     
        //PROTOBUF

        private CommandInfo ParseCommand(Alachisoft.NCache.Common.Protobuf.Command command, ClientManager clientManager)
        {
            CommandInfo cmdInfo = new CommandInfo();

            Alachisoft.NCache.Common.Protobuf.GetEnumeratorCommand getEnumeratorCommand = command.getEnumeratorCommand;

            cmdInfo.RequestId = getEnumeratorCommand.requestId.ToString();

            return cmdInfo;
        }       
    }
}
