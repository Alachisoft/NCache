using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alachisoft.NCache.SocketServer.MultiBufferSend
{
    public class ResponseBuffers
    {
        private int _index;

        private readonly IList _buffers;

        internal ResponseBuffers(IList buffers)
        {
            _buffers = buffers;
            _index = 0;
            Size = CalculateSize(buffers);
            CreationTime = DateTime.UtcNow;
        }

        internal int Size { get; }

        internal DateTime CreationTime { get; }

        internal bool WriteBuffers(byte[] sendBuffer, ref int offset)
        {
            while (_index < _buffers.Count)
            {
                byte[] responseBuffer = (byte[])_buffers[_index];
                if (responseBuffer.Length + offset < sendBuffer.Length)
                {
                    Buffer.BlockCopy(responseBuffer, 0, sendBuffer, offset, responseBuffer.Length);
                    _index++;
                    offset += responseBuffer.Length;
                }
                else
                    return false;
            }

            return true;
        }


        internal IList GetBuffer()
        {
            return _buffers;
        }

        private int CalculateSize(IList buffers)
        {
            int size = 0;

            for (int i = 0; i < buffers.Count; i++)
            {
                size += ((byte[])buffers[i]).Length;
            }

            return size;
        }
    }
}
