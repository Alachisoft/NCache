#if SERVER || NETCORE
using System;
using System.Buffers;
using System.Linq;

namespace Alachisoft.NCache.SocketServer.Pipelining
{
    internal class RequestInfo
    {
        int _remainingLength;

        public bool IsCompleted { get { return _remainingLength == 0; } }

        public byte[] DataBuffer { get; }

        public RequestInfo(byte[] buffer, int remainingLength)
        {
            DataBuffer = buffer;
            _remainingLength = remainingLength;
        }

        internal void Read(ref ReadOnlySequence<byte> buffer)
        {
            int count = buffer.Length >= _remainingLength ? _remainingLength : (int)buffer.Length;

            byte[] data = buffer.Slice(0, count).ToArray();
            Buffer.BlockCopy(data, 0, DataBuffer, DataBuffer.Length - _remainingLength, count);
            buffer = buffer.Slice(count, buffer.End);
            _remainingLength -= data.Length;
        }
    }
}
#endif


