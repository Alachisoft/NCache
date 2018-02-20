using Alachisoft.NCache.Common.Sockets;

namespace Alachisoft.NCache.SocketServer
{
    internal sealed class RecContextServer : ReceiveContext
    {
        public bool IsOptimized;
        public uint RequestId;
        public int ChunkToReceive;
        public long AcknowledgmentId;
        public ClientManager Client;
    }
}
