
using System;
using System.Text;
using System.Net.Sockets;

namespace Memcached.ClientLibrary
{
    public class NetworkStreamIgnoreSeek : NetworkStream
    {
        public NetworkStreamIgnoreSeek(Socket socket, System.IO.FileAccess access, bool ownsSocket)
            : base(socket, access, ownsSocket) { }

        public NetworkStreamIgnoreSeek(Socket socket, System.IO.FileAccess access)
            : base(socket, access) { }

        public NetworkStreamIgnoreSeek(Socket socket, bool ownsSocket)
            : base(socket, ownsSocket) { }

        public NetworkStreamIgnoreSeek(Socket socket)
            : base(socket) { }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            //ignore this.  If we wrap this stream with a 
            //BufferedStream this should prevent the underlying 
            //NetworkStream from throwing an exception when Flush()
            //is called
            return 0;
        }
    }
}
