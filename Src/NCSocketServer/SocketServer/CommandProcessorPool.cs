// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.SocketServer.Statistics;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NCache.SocketServer
{
    internal class CommandProcessorPool
    {
        private readonly int _maxProcessors;
        private readonly CommandProcessor[] _workers;

        public CommandProcessorPool(int processors, IRequestProcessor reqProcessor, PerfStatsCollector collector)
        {
            _maxProcessors = processors;
            _workers = new CommandProcessor[processors];

            for (int i = 0; i < processors; i++){
                _workers[i] = new CommandProcessor(reqProcessor, collector);
            }
        }

        public void EnqueuRequest(ProcCommand request, uint indexFeed)
        {
            _workers[indexFeed % _maxProcessors].EnqueuRequest(request);
        }

        public void Start()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Start();
                }
            }
        }

        public void Stop()
        {
            lock (this){
                for (int i = 0; i < _maxProcessors; i++){
                    _workers[i].Stop();
                }
            }
        }
    }

    internal class CommandProcessor
    {
        private bool _workerWaiting;
        private readonly Thread _worker;
        private readonly IRequestProcessor _processor;
        private readonly Queue<ProcCommand> _commandsQueue;
        private readonly SemaphoreLock _semaLock = new SemaphoreLock();
        private readonly object _queueLock = new object();

        public CommandProcessor(IRequestProcessor processor, PerfStatsCollector collector)
        {
            _processor = processor;

            _commandsQueue = new Queue<ProcCommand>();

            _worker = new Thread(Run);
            _worker.IsBackground = true;
            _worker.Priority = ThreadPriority.AboveNormal;           

        }

        public void EnqueuRequest(ProcCommand request)
        {
            _semaLock.Enter();
            _commandsQueue.Enqueue(request);
            if (_workerWaiting){
                lock (_queueLock){
                    Monitor.Pulse(_queueLock);
                }
            }
            _semaLock.Exit();
        }

        private void Run()
        {
            ProcCommand command = null;
            try
            {
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
                    command = _commandsQueue.Dequeue();
                    _semaLock.Exit();
                    _processor.Process(command);
                }
            }
            catch (Exception){
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
            if (_worker != null && _worker.IsAlive){
                _worker.Abort();
            }
        }
    }
}
