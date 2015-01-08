using System;
using System.Text;
using Alachisoft.NCache.Common.Net;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.EventTask
{
    internal sealed class NodeLeftEvent : IEventTask
    {
        private string _cacheId;
        private Address _clusterAddress;
        private Address _serverAddress;
        private string _clientId;

        internal NodeLeftEvent(string cacheId, Address clusterAddress, Address serverAddress, string clientid)
        {
            _cacheId = cacheId;
            _clusterAddress = clusterAddress;
            _serverAddress = serverAddress;
            _clientId = clientid;
        }

        public void Process()
        {
            ClientManager clientManager = null;

            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.NodeLeftEventResponse nodeLeft = new Alachisoft.NCache.Common.Protobuf.NodeLeftEventResponse();

                nodeLeft.clusterIp = _clusterAddress.IpAddress.ToString();
                nodeLeft.clusterPort = _clusterAddress.Port.ToString();
                nodeLeft.serverIp = _serverAddress.IpAddress.ToString();
                nodeLeft.serverPort = _serverAddress.Port.ToString();

                response.nodeLeft = nodeLeft;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.NODE_LEFT_EVENT;

                byte[] serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Critical);
            }
        }
    }
}
