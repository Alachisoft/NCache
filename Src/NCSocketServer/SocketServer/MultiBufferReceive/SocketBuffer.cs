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
using System.Net.Sockets;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal sealed class SocketBuffer : IDisposable
    {
        private readonly object _mutex;

        private bool _busyReceiving;

        private IBufferListener _bufferListener;
        internal byte[] Buffer { get; private set; }
        internal long ReadIndex { get; private set; } = 0;
        internal long WriteIndex { get; private set; } = 0;

        internal int CommandBufferCount { get; private set; } = 0;
        internal SocketAsyncEventArgs SocketEventArgs { get; private set; }

        internal SocketBuffer(byte[] buffer, IBufferListener bufferListener, SocketAsyncEventArgs socketArgs)
        {
            _mutex = new object();
            _bufferListener = bufferListener;
            Buffer = buffer;
            SocketEventArgs = socketArgs;
        }

        internal long UnreadBytes
        {
            get { lock (_mutex) return WriteIndex - ReadIndex; }
        }

        internal bool BusyReceiving
        {
            get { lock (_mutex) return _busyReceiving; }
            set { lock (_mutex) _busyReceiving = value; }
        }

        internal void BytesWritten(long receivedBytes)
        {
            lock (_mutex) { WriteIndex += receivedBytes; }
        }

        internal void BytesRead(long readBytes)
        {
            lock (_mutex) { ReadIndex += readBytes; }
        }

        internal void Clear()
        {
            lock (_mutex) { ReadIndex = WriteIndex = CommandBufferCount = 0; }
        }

        public void Dispose()
        {
            lock (_mutex) { SocketEventArgs.Dispose(); }
        }

        internal void IncrementCommandBufferCount()
        {
            lock(_mutex)
            {
                CommandBufferCount++;
            }
        }

        internal void DecrementCommandBufferCount()
        {
            lock (_mutex)
            {
                CommandBufferCount--;
                FreeBuffer();
            }
        }

        internal void FreeBuffer()
        {
            lock (_mutex)
            {
                if (!_busyReceiving && CommandBufferCount == 0)
                {
                    Clear();
                    _bufferListener.OnBufferFree(this);
                }
            }
        }
    }
}
