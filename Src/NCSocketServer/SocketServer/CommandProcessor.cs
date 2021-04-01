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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.Locking;
using Alachisoft.NCache.SocketServer.Statistics;

namespace Alachisoft.NCache.SocketServer
{
    internal class CommandProcessor
    {
        private bool _workerWaiting;

        private readonly Thread _worker;
        private readonly IRequestProcessor _processor;
        private readonly ConcurrentQueue<ProcCommand> _commandsQueue;

#if NET40
        private readonly SemaphoreLock _semaLock = new SemaphoreLock();
#else
        private readonly object _semaLock = new object();
#endif



        private readonly object _queueLock = new object();

        public CommandProcessor(IRequestProcessor processor, StatisticsCounter collector)
        {
            _processor = processor;
            _commandsQueue = new ConcurrentQueue<ProcCommand>();

            _worker = new Thread(Run);
            _worker.IsBackground = true;
            _worker.Priority = ThreadPriority.AboveNormal;
        }

        public void EnqueuRequest(ProcCommand request)
        {
#if NET40
            _semaLock.Enter();

            _commandsQueue.Enqueue(request);

            if (_workerWaiting)
            {
                lock (_queueLock)
                {
                    Monitor.Pulse(_queueLock);
                }
            }

            _semaLock.Exit();
#else
            
              _commandsQueue.Enqueue(request);
            if (_workerWaiting)
            {
                lock (_queueLock)
                {
                    if (_workerWaiting)
                        Monitor.Pulse(_queueLock);
                }
            }
#endif
        }

        private void Run()
        {
            ProcCommand command = null;
            try
            {

#if NET40

                while (true)
                {
                    _semaLock.Enter();

                    if (_commandsQueue.Count == 0)
                    {
                        lock (_queueLock)
                        {
                            _workerWaiting = true;

                            _semaLock.Exit();

                            Monitor.Wait(_queueLock);
                        }

                        _semaLock.Enter();

                        _workerWaiting = false;
                    }

                    _commandsQueue.TryDequeue(out command);

                    _semaLock.Exit();

                    _processor.Process(command);
                }
#else
                while (true)
                {
                    if (!_commandsQueue.TryDequeue(out command))
                    {
                        lock (_queueLock)
                        {
                            if (_commandsQueue.Count == 0)
                            {
                                _workerWaiting = true;
                                Monitor.Wait(_queueLock);

                            }
                            _workerWaiting = false;
                        }
                        continue;
                    }
                    _processor.Process(command);
                }
#endif
            }
            catch (Exception)
            {
                if (command != null && command.ClientManager != null)
                    command.ClientManager.Dispose();
            }
        }

        public void Start()
        {
            _worker.Start();
        }

        public void Stop()
        {
            if (_worker != null && _worker.IsAlive)
            {
#if !NETCORE
                _worker.Abort();
#elif NETCORE
                _worker.Interrupt();
#endif
            }
        }
    }
}