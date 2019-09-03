using Alachisoft.NCache.Common.Locking;
using System;
using System.Collections.Generic;
using System.Threading;
using Alachisoft.NCache.Common.Locking;

namespace Alachisoft.NGroups.Util
{
    internal class EventProcessor
    {
        private bool _workerWaiting;

        private readonly Thread _worker;        
        private readonly Queue<IEvent> _eventsQueue;

#if NET40
        private readonly SemaphoreLock _semaLock = new SemaphoreLock();
#else
        private readonly object _semaLock = new object();
#endif



        private readonly object _queueLock = new object();

        public EventProcessor()
        {
            _eventsQueue = new Queue<IEvent>();

            _worker = new Thread(Run);
            _worker.IsBackground = true;
            _worker.Priority = ThreadPriority.AboveNormal;
        }

        public void EnqueueEvent(IEvent request)
        {
#if NET40
            try
            {
                _semaLock.Enter();

                _eventsQueue.Enqueue(request);

                if (_workerWaiting)
                {
                    lock (_queueLock)
                    {
                        Monitor.Pulse(_queueLock);
                    }
                }
            }
            finally
            {
                _semaLock.Exit();
            }
#else
            lock (_queueLock)
            {
                _eventsQueue.Enqueue(request);
                if (_workerWaiting)
                {
                    Monitor.Pulse(_queueLock);
                }
            }
#endif
        }

        private void Run()
        {
            IEvent e = null;
            try
            {

#if NET40
                
                while (true)
                {
                    try
                    {
                        _semaLock.Enter();

                        if (_eventsQueue.Count == 0)
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
                        e = _eventsQueue.Dequeue();

                        //_semaLock.Exit();
                    }
                    finally 
                    {                        
                        _semaLock.Exit();
                    }

                    try
                    {
                        e.Process();
                    }
                    catch (Exception)
                    {
                    }                   
                }
#else
                while (true)
                {
                    lock (_queueLock)
                    {
                        if (_eventsQueue.Count == 0)
                        {
                            _workerWaiting = true;
                            Monitor.Wait(_queueLock);
                        }
                        _workerWaiting = false;
                        e = _eventsQueue.Dequeue();
                    }
                    try
                    {
                        e.Process();
                    }
                    catch (Exception)
                    { 
                    }
                }
#endif
            }
            catch (Exception)
            {
                
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