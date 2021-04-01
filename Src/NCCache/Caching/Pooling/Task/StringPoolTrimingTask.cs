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

using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Pooling
{

    /// <summary>
    /// The Task that takes care of trimming StringPool.
    /// </summary>
    class StringPoolTrimmingTask : TimeScheduler.Task
    {
        /// <summary> Reference to the parent. </summary>
        private IStringPool _parent = null;

        /// <summary> Reference to the CacheRuntimeContext. </summary>
        private CacheRuntimeContext _cacheRuntimeContext = null;

        /// <summary> Triming interval </summary>
        private long _interval;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="cacheRuntimeContext"></param>
        /// <param name="interval"></param>
        internal StringPoolTrimmingTask(CacheRuntimeContext cacheRuntimeContext, long interval = 300000) // Default: 5 minutes = 5*60*1000
        {
            _parent = cacheRuntimeContext.TransactionalPoolManager.StringPool;
            _cacheRuntimeContext = cacheRuntimeContext;
            _interval = interval; 
        }

        /// <summary>
        /// Returns true if the task has completed.
        /// </summary>
        /// <returns>bool</returns>
        bool TimeScheduler.Task.IsCancelled()
        {
            lock (this) { return (_parent == null || _cacheRuntimeContext == null); }
        }

        /// <summary>
        /// Tells the scheduler about next interval.
        /// </summary>
        /// <returns></returns>
        long TimeScheduler.Task.GetNextInterval()
        {
            lock (this) { return _interval; }
        }

        /// <summary>
        /// This is the main method that runs as a thread. 
        /// </summary>
        void TimeScheduler.Task.Run()
        {
            if (_parent == null) return;
            if (_cacheRuntimeContext == null) return;
            if (_cacheRuntimeContext.CacheInternal == null) return;

            try
            {
                _parent.TrimPool(_cacheRuntimeContext.CacheInternal.Count);
            }
            catch { }
        }
    }
}
