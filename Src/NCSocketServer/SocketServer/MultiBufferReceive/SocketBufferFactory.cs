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

using System.Net.Sockets;

namespace Alachisoft.NCache.SocketServer.MultiBufferReceive
{
    internal sealed class SocketBufferFactory
    {
        private readonly int _bufferSize;
        private readonly ClientManager _clientManger;

        internal SocketBufferFactory(int bufferSize, ClientManager clientManager)
        {
            _bufferSize = bufferSize;
            _clientManger = clientManager;
        }

        internal SocketBuffer CreateSocketBuffer(IBufferListener bufferListener)
        {
            var buffer = new byte[_bufferSize];
            var eventArgs = new SocketAsyncEventArgs();
            eventArgs.RemoteEndPoint = _clientManger.ClientSocket.RemoteEndPoint;
            eventArgs.Completed += _clientManger.ConnectionManager.OnReceiveBuffer;
            eventArgs.SetBuffer(buffer, 0, 0);
            return new SocketBuffer(buffer, bufferListener, eventArgs);
        }
    }
}
