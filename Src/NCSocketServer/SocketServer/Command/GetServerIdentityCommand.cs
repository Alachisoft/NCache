using System;
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common.ResponseSerialization;

namespace Alachisoft.NCache.SocketServer.Command
{
    class GetServerIdentityCommand : CommandBase
    {
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            string requestId;
            try
            {
                Alachisoft.NCache.Common.Protobuf.GetServerIdentityCommand getServerIdentityCommand = command.getServerIdentityCommand;

                requestId = getServerIdentityCommand.requestId.ToString();
            }
            catch (Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                {
                    ResponseOptions responseOptions = new ResponseOptions()
                    {
                        Exception = exc,
                        RequestId = command.requestID,
                        CommandId = command.commandID,
                    };

                    _serializedResponsePackets.Add(clientManager.ResponseBuilder.BuildExceptionResponse(responseOptions));
                }
                return;
            }

            Alachisoft.NCache.Caching.Cache cache = null;

            try
            {
                string server = ConnectionManager.ServerIpAddress;

                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.GetServerIdentityResponse getServerIdentityResponse = new Alachisoft.NCache.Common.Protobuf.GetServerIdentityResponse();
                getServerIdentityResponse.server = server;
                response.commandID = command.commandID;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.GET_SERVER_IDENTITY;
                response.getServerIdentityResponse = getServerIdentityResponse;


                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response));
            }
            catch (Exception exc)
            {
                if (SocketServer.Logger != null && SocketServer.Logger.IsErrorLogsEnabled)
                    SocketServer.Logger.NCacheLog.Error(nameof(GetServerIdentityCommand), clientManager.ToString() + "Error " + exc);
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithoutType(exc, command.requestID, command.commandID));
            }
        }




    }
}
