//  Copyright (c) 2021 Alachisoft
//  
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//  
//     http://www.apache.org/licenses/LICENSE-2.0
//  
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal sealed class CommandStream : Stream
    {
        private long _position;
        private long _commandLength;
        private int _length;

        LinkedList<CommandBuffer> _buffers = new LinkedList<CommandBuffer>();

        #region  /                       --- Stream Implementation ---                   /

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanSeek
        {
            get { return false; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return _commandLength; }
        }

        public long AvailableData { get { return _length; } }

        public override long Position
        {
            get { return _position; }
            set { _position = value; }
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public new void Dispose()
        {
            base.Dispose();
            lock (_buffers)
                _buffers.Clear();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = 0;
            if (AllowedLength <= 0) return bytesRead;

            int removeCount = 0;

            LinkedListNode<CommandBuffer> node = _buffers.First;
            while (node != null && bytesRead <= count && AllowedLength > 0)
            {
                CommandBuffer cmdBuffer = node.Value;

                int unreadData = cmdBuffer.BufferSegment.Count - cmdBuffer.Index;
                int dataToRead = count - bytesRead > unreadData ? unreadData : count - bytesRead;

                if (dataToRead > AllowedLength) dataToRead = AllowedLength;

                Buffer.BlockCopy(cmdBuffer.BufferSegment.Array,
                                 cmdBuffer.BufferSegment.Offset + cmdBuffer.Index,
                                 buffer, offset, dataToRead);

                offset += dataToRead;
                cmdBuffer.Index += dataToRead;
                bytesRead += dataToRead;

                _position += dataToRead;

                if (cmdBuffer.Index == cmdBuffer.BufferSegment.Count)
                {
                    cmdBuffer.Buffer.DecrementCommandBufferCount();
                    removeCount++;
                }

                AllowedLength -= dataToRead;

                node = node.Next;
            }

            lock (_buffers)
            {
                _length -= bytesRead;
            }
            if (removeCount > 0)
            {
                lock (_buffers)
                {
                    while (removeCount > 0)
                    {
                        _buffers.RemoveFirst();
                        removeCount--;
                    }
                }
            }

            return bytesRead;
        }
        
        #endregion

        public int AllowedLength { get; private set; }

        public long CommandLength
        {
            get { return _commandLength; }
            set
            {
                _commandLength = value;
                AllowedLength = (int)value;
            }
        }

        public void AddCommandBuffer(CommandBuffer commandBuffer)
        {
            lock (_buffers)
            {
                _buffers.AddLast(commandBuffer);
                _length += commandBuffer.BufferSegment.Count;
            }

        }

        public bool EnsureData(int count)
        {
            lock (_buffers)
            {
                return _length - count >= 0;
            }
            return count <= 0;
        }

        internal bool HasAnyData()
        {
            lock (_buffers)
            {
                return _length > 0;
            }
        }
    }
}
