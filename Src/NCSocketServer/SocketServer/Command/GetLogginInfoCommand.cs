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
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetLogginInfoCommand : CommandBase
    {

        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            ///Command:
            /// GETLOGGINGINFO "requestId"
            /// 
            string requestId = string.Empty;

            Alachisoft.NCache.Common.Protobuf.GetLoggingInfoCommand getLoggingInfoCommand = command.getLoggingInfoCommand;
            requestId = getLoggingInfoCommand.requestId.ToString();

            bool errorEnabled = ConnectionManager.GetClientLoggingInfo(LoggingInfo.LoggingType.Error) == LoggingInfo.LogsStatus.Enable;
            bool detailedEnabled = ConnectionManager.GetClientLoggingInfo(LoggingInfo.LoggingType.Detailed) == LoggingInfo.LogsStatus.Enable;

            if (!errorEnabled)
                detailedEnabled = false;

            try
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetLoggingInfoResponse loggingInfoResponse = new Alachisoft.NCache.Common.Protobuf.GetLoggingInfoResponse();
                response.requestId = command.requestID;
                response.commandID = command.commandID;
                loggingInfoResponse.errorsEnabled = errorEnabled;
                loggingInfoResponse.detailedErrorsEnabled = detailedEnabled;

                response.getLoggingInfoResponse = loggingInfoResponse;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_LOGGING_INFO;

                //_resultPacket = clientManager.ReplyPacket("GETLOGGINGINFORESULT \"" + requestId + "\"" + errorEnabled + "\"" + detailedEnabled + "\"", new byte[0]);
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response,Common.Protobuf.Response.Type.GET_LOGGING_INFO));
            }
            catch (Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

    }
}