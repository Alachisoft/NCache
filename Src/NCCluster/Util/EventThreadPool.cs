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
using System.Linq;
using System.Text;
using System.Threading;
using Alachisoft.NGroups.Stack;
using Alachisoft.NCache.Common.Util;

#if NET40
using Alachisoft.NCache.Common.Locking;
#endif

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