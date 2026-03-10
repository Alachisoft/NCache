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

using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.ResponseSerialization;

namespace Alachisoft.NCache.SocketServer.Command
{
    internal class GetSerializationFormatCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Common.Protobuf.Command command)
        {
            try
            {
                NCache cache = clientManager.CmdExecuter as NCache;
                SerializationFormat serializationFormat = cache.Cache.SerializationFormat;

                GetSerializationFormatResponse serializationFormatResponse = new GetSerializationFormatResponse()
                {
                    serializationFormat = (int)serializationFormat
                };

                if (clientManager.ClientVersion >= 5000)
                {
                    Common.Util.ResponseHelper.SetResponse(serializationFormatResponse, command.requestID, command.commandID);
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = serializationFormatResponse,
                        ResponseType = Response.Type.GET_SERIALIZATION_FORMAT
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }
                else
                {
                    //PROTOBUF:RESPONSE
                    Response response = new Response();
                    response.getSerializationFormatResponse = serializationFormatResponse;
                    Common.Util.ResponseHelper.SetResponse(response, command.requestID, command.commandID, Response.Type.GET_SERIALIZATION_FORMAT);
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Response = response,
                        ResponseType = Response.Type.GET_SERIALIZATION_FORMAT
                    };
                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildResponse(responseOptions));
                }
            }
            catch(System.Exception ex)
            {
                ResponseOptions responseOptions = new ResponseOptions()
                {
                    Exception = ex,
                    RequestId = command.requestID,
                    CommandId = command.commandID,
                };

                _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
            }
        }
    }
}
