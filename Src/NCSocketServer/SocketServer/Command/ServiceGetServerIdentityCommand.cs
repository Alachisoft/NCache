using System;
using System.Text;

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.SocketServer.Util;
using System.Collections.Generic;
using Alachisoft.NCache.Common.ResponseSerialization;
using Alachisoft.NCache.Common.Protobuf;

namespace Alachisoft.NCache.SocketServer.Command
{
    class ServiceGetServerIdentityCommand : CommandBase
    {
        private long acknowledgementId;

        public ServiceGetServerIdentityCommand(long acknowledgementId)
        {
            this.acknowledgementId = acknowledgementId;
        }


        //PROTOBUF
        public override void ExecuteCommand(ClientManager clientManager, Alachisoft.NCache.Common.Protobuf.Command command)
        {
            string requestId;
            try
            {
                Alachisoft.NCache.Common.Protobuf.GetServerIdentityCommand getServerIdentityCommand = command.getServerIdentityCommand;

                requestId = getServerIdentityCommand.requestId.ToString();
            }
            catch (System.Exception exc)
            {
                if (!base.immatureId.Equals("-2"))
                    _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
                return;
            }
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
            catch (System.Exception exc)
            {
                _serializedResponsePackets.Add(Alachisoft.NCache.Common.Util.ResponseHelper.SerializeExceptionResponseWithType(exc, command.requestID, command.commandID, clientManager.ClientVersion));
            }
        }

    }

}
