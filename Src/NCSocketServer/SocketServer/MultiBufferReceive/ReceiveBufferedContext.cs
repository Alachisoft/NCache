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
    internal abstract class BufferedSocketContext : IDisposable
    {
        internal BufferedSocketContext(SocketBufferManager buffManager, ClientManager clientManager)
        {
            BufferManager = buffManager;
            ClientManager = clientManager;
        }

        protected SocketBuffer Buffer { get; set; }
        protected SocketBufferManager BufferManager { get; private set; }

        internal ClientManager ClientManager { get; private set; }
        internal SocketAsyncEventArgs SocketEventArgs { get { return Buffer.SocketEventArgs; } }

        public virtual void Dispose()
        {
            BufferManager.Dispose();
        }
    }

    internal sealed class ReceiveBufferedContext : BufferedSocketContext
    {
        internal ReceiveBufferedContext (SocketBufferManager buffManager,
            ClientManager clientManager) : base(buffManager, clientManager) { }

        internal RequestDeserializer Deserializer { get { return ClientManager.RequestDeserializer; } }

        internal CommandBuffer UpdateBytesReceived()
        {
            CommandBuffer cmdBuffer = new CommandBuffer();
            Buffer.BytesWritten(Buffer.SocketEventArgs.BytesTransferred);
            ClientManager.AddToClientsBytesRecieved(Buffer.SocketEventArgs.BytesTransferred);
            cmdBuffer.Buffer = Buffer;
            cmdBuffer.BufferSegment = new ArraySegment<byte>(Buffer.Buffer, Buffer.SocketEventArgs.Offset, Buffer.SocketEventArgs.BytesTransferred);
            Buffer.IncrementCommandBufferCount();
            return cmdBuffer;
        }

        internal void PrepareReceive()
        {
            if (Buffer == null || Buffer.Buffer.Length - Buffer.WriteIndex < 2048)
            {
                if (Buffer != null)
                {
                    Buffer.BusyReceiving = false;
                    Buffer.FreeBuffer();
                }

                SocketBuffer socketBuffer;
                if (BufferManager.GetFreeOrNewBuffer(out socketBuffer))
                {
                    Buffer = socketBuffer;
                    Buffer.BusyReceiving = true;
                }
            }

            SocketEventArgs.SetBuffer((int)Buffer.WriteIndex, (int)(Buffer.Buffer.Length - Buffer.WriteIndex));
            SocketEventArgs.UserToken = this;
            // Do other necessary markings here.


        }
    }
}
