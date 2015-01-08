using System;
using System.Text;
using Alachisoft.NCache.Common.Net;
using System.Collections;

namespace Alachisoft.NCache.SocketServer.EventTask
{
	internal sealed class NodeJoinedEvent : IEventTask
	{
		private string _cacheId;
		private Address _clusterAddress;
        private Address _serverAddress;
        private string _clientId;
        private bool _reconnect;

		internal NodeJoinedEvent(string cacheId, Address clusterAddress, Address serverAddress, string clientid, bool reconn)
		{
			_cacheId = cacheId;
            _clusterAddress = clusterAddress;
            _serverAddress = serverAddress;
            _clientId = clientid;
            _reconnect = reconn;
		}

        public void Process()
        {
            ClientManager clientManager = null;


            lock (ConnectionManager.ConnectionTable) clientManager = (ClientManager)ConnectionManager.ConnectionTable[_clientId];
            if (clientManager != null)
            {
                Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
                Alachisoft.NCache.Common.Protobuf.NodeJoinedEventResponse nodeJoined = new Alachisoft.NCache.Common.Protobuf.NodeJoinedEventResponse();

                nodeJoined.clusterIp = _clusterAddress.IpAddress.ToString();
                nodeJoined.clusterPort = _clusterAddress.Port.ToString();
                nodeJoined.serverIp = _serverAddress.IpAddress.ToString();
                nodeJoined.serverPort = _serverAddress.Port.ToString();
                nodeJoined.reconnect = _reconnect;

                response.nodeJoined = nodeJoined;
                response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.NODE_JOINED_EVENT;

                byte[] serializedResponse = Alachisoft.NCache.Common.Util.ResponseHelper.SerializeResponse(response);

                ConnectionManager.AssureSend(clientManager, serializedResponse, Alachisoft.NCache.Common.Enum.Priority.Critical);
            }
        }
	}

	
}
