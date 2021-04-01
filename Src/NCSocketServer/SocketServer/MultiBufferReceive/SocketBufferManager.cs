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
using System.Threading;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal sealed class SocketBufferManager : IBufferListener, IDisposable
    {
        private readonly int
            _minBuffers,
            _maxBuffers,
            _bufferSize;

        private readonly Queue<SocketBuffer> _freeBuffers;
        private readonly IList<SocketBuffer> _busyBuffers;

        private readonly SocketBufferFactory _bufferFactory;
        private bool _isDisposed;
        private readonly object _mutex;

        public SocketBufferManager(int minBuffers, int maxBuffers, int bufferSize, ClientManager clientManager)
        {
            _mutex = new object();

            _freeBuffers = new Queue<SocketBuffer>();
            _busyBuffers = new List<SocketBuffer>();

            _minBuffers = minBuffers;
            _maxBuffers = maxBuffers;
            _bufferSize = bufferSize;

            _bufferFactory = new SocketBufferFactory(bufferSize, clientManager);

            for (int i = 0; i < _minBuffers; i++)
                _freeBuffers.Enqueue(_bufferFactory.CreateSocketBuffer(this));
        }

        internal bool GetFreeOrNewBuffer(out SocketBuffer socketBuffer)
        {
            socketBuffer = null;

            lock (_mutex)
            {
                while (_busyBuffers.Count == _maxBuffers && !_isDisposed)
                    Monitor.Wait(_mutex);

                if (_freeBuffers.Count == 0)
                    _freeBuffers.Enqueue(_bufferFactory.CreateSocketBuffer(this));

                socketBuffer = _freeBuffers.Dequeue();
                _busyBuffers.Add(socketBuffer);
                return true;
            }
        }

        internal bool GetBusyBufferAt(int index, out SocketBuffer buffer)
        {
            buffer = null;
            lock (_mutex)
            {
                if (index >= _busyBuffers.Count) return false;
                buffer = _busyBuffers[index];
                return true;
            }
        }

        internal bool FreeTopBusyBuffer()
        {
            lock (_mutex)
            {
                if (_busyBuffers.Count > 0)
                {
                    SocketBuffer buffer = _busyBuffers[0];
                    buffer.Clear();
                    _busyBuffers.RemoveAt(0);
                    _freeBuffers.Enqueue(buffer);

                    Monitor.Pulse(_mutex);
                    return true;
                }
                return false;
            }
        }
        
        public void OnBufferFree(SocketBuffer buffer)
        {
            lock (_mutex)
            {
                if (_busyBuffers.Count > 0)
                {
                    if (_busyBuffers.Remove(buffer))
                    {
                        _freeBuffers.Enqueue(buffer);
                        Monitor.Pulse(_mutex);
                    }
                }
            }
        }

        public void Dispose()
        {
            lock (_mutex)
            {
                foreach (var buffer in _freeBuffers)
                    buffer.Dispose();

                foreach (var buffer in _busyBuffers)
                    buffer.Dispose();

                _freeBuffers.Clear();
                _busyBuffers.Clear();

                _isDisposed = true;
                Monitor.Pulse(_mutex);
            }
        }
        
    }
}
