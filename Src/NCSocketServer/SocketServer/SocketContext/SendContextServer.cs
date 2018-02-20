using System;
using Alachisoft.NCache.Common.Sockets;

namespace Alachisoft.NCache.SocketServer
{
    internal sealed class SendContextServer : SendContext
    {
        public ArraySegment<byte>[] Buffers;
        public ClientManager Client;
    }
}
