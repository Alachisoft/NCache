

#if NETCORE 
using Microsoft.IO;
#endif

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer
{
    public sealed class StreamManager
    {

#if NETCORE
        private static readonly RecyclableMemoryStreamManager recyclableStreamManager = new RecyclableMemoryStreamManager();
#endif
        public static MemoryStream GetStream(byte[] buffer = null)
        {
#if !NETCORE
               return buffer !=null ? new MemoryStream(buffer) : new MemoryStream();
#else
            return buffer != null ? recyclableStreamManager.GetStream(buffer) : recyclableStreamManager.GetStream();

#endif
        }
    }
}
