using System;
using System.Threading;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NGroups.Util
{
    internal class EventThreadPool
    {
        private long indexFeed = 0;
        private readonly long _maxProcessors;

        private readonly EventProcessor[] _workers;
        private static EventThreadPool _pool;
        private static Object mutex = new Object();
        
        public static EventThreadPool Instance
        {
            get
            {
                lock (mutex)
                {
                    if (_pool == null)
                    {
                        _pool = new EventThreadPool(Environment.ProcessorCount * ServiceConfiguration.EventThreadPoolCount);
                        _pool.Start();
                    }
                }
                return _pool;
            }
        }

        public static void StopPool()         
        {
            lock (mutex)
            {
                if (_pool != null)
                {
                    _pool.Stop();                    
                }
                _pool = null;
            }
        }

        private EventThreadPool(int processors)
        {
            _maxProcessors = processors;
            _workers = new EventProcessor[processors];

            for (int i = 0; i < processors; i++)
            {
                _workers[i] = new EventProcessor();
            }
        }

        public void EnqueueEvent(IEvent request)
        {
            Interlocked.Increment(ref indexFeed);

            _workers[indexFeed % _maxProcessors].EnqueueEvent(request);
        }

        public void Start()
        {
            lock (this)
            {
                for (int i = 0; i < _maxProcessors; i++)
                {
                    _workers[i].Start();
                }
            }
        }

        public void Stop()
        {
            lock (this)
            {
                for (int i = 0; i < _maxProcessors; i++)
                {
                    _workers[i].Stop();
                }
            }
        }
    }
}