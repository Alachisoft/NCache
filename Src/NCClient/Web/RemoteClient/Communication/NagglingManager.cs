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

using Alachisoft.NCache.Common.Threading;
using System;
using System.Collections;
using System.Net.Sockets;
using System.Threading;

namespace Alachisoft.NCache.Client
{
    internal class NagglingManager : ThreadClass
    {
        private Alachisoft.NCache.Common.DataStructures.Queue _msgQueue;
        private long _nagglingSize;
        private Connection _parent;
        private Socket _workingSocket;
        private object _syncLock;
        private int _sendBufferSize = 1024 * 1024;
        private byte[] _sendBuffer;
        private long _waitTimeout = 0;

        internal NagglingManager(Connection parent, Socket workingSocket, Alachisoft.NCache.Common.DataStructures.Queue msgQueue, long nagglingSize, object syncLock)
        {
            _parent = parent;
            _workingSocket = workingSocket;
            _nagglingSize = nagglingSize;
            _msgQueue = msgQueue;
            _sendBuffer = new byte[_sendBufferSize];
            _syncLock = syncLock;
        }

        public override void Run()
        {
            try
            {
                ArrayList msgList = new ArrayList();
                byte[] tmpBuffer;
                int totalMsgSize = 0;
                int offset = 0;
                ArrayList msgsTobeSent = new ArrayList();
                while (!_msgQueue.Closed)
                {
                    try
                    {
                        msgsTobeSent.Clear();
                        lock (_syncLock)
                        {
                            tmpBuffer = _sendBuffer;
                            totalMsgSize = 0;
                            offset = 0;
                            while (true)
                            {
                                byte[] msg = (byte[])_msgQueue.remove();

                                if (msg != null)
                                {
                                    msgsTobeSent.Add(msg);
                                    totalMsgSize += msg.Length;

                                    if (totalMsgSize > _sendBuffer.Length)
                                        tmpBuffer = new byte[totalMsgSize];

                                    Buffer.BlockCopy(msg, 0, tmpBuffer, offset, msg.Length);
                                    offset += msg.Length;
                                }
                                msg = null;
                                try
                                {
                                    bool success = false;
                                    msg = _msgQueue.peek(_waitTimeout, out success) as byte[];
                                }
                                catch (Alachisoft.NCache.Common.Exceptions.TimeoutException)
                                {

                                }
                                if ((msg == null || ((msg.Length + totalMsgSize) > _nagglingSize))) break;

                            }
                        }
                        _parent.AssureSend(_workingSocket, tmpBuffer, totalMsgSize, true);
                    }
                    catch (ThreadInterruptedException e)
                    {
                        break;
                    }
                    catch (System.Exception e)
                    {
                    }
                }
            }
            catch (ThreadInterruptedException e)
            {
            }
            catch (Exception e)
            {
            }
        }
    }
}
