using Alachisoft.NCache.Common.Locking;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Alachisoft.NCache.SocketServer.Pipelining
{
    internal class CommandProcessor
    {
        private bool _workerWaiting;

        private readonly Thread _worker;
        private readonly IRequestProcessor _processor;
        private readonly ConcurrentQueue<LongRunningCommand> _commandsQueue;

#if NET40
        private readonly SemaphoreLock _semaLock = new SemaphoreLock();
#else
        private readonly object _semaLock = new object();
#endif

        private readonly object _queueLock = new object();

        public CommandProcessor(IRequestProcessor processor)
        {
            _processor = processor;
            _commandsQueue = new ConcurrentQueue<LongRunningCommand>();

            _worker = new Thread(Run)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
        }

        public void EnqueuRequest(LongRunningCommand request)
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
            LongRunningCommand command = null;
            
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
