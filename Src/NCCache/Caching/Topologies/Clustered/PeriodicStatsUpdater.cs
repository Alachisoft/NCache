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

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    class PeriodicStatsUpdater : TimeScheduler.Task
    {
        /// <summary> The parent on this task. </summary>
        private ClusterCacheBase _parent = null;

        /// <summary> The periodic interval for stat replications. </summary>
        private long _period = 10000;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public PeriodicStatsUpdater(ClusterCacheBase parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="period"></param>
        public PeriodicStatsUpdater(ClusterCacheBase parent, long period)
        {
            _parent = parent;
            _period = period;
        }

        /// <summary>
        /// Sets the cancell flag.
        /// </summary>
        public void Cancel()
        {
            lock (this) { _parent = null; }
        }

        /// <summary>
        /// The task is cancelled or not. 
        /// </summary>
        /// <returns></returns>
        bool TimeScheduler.Task.IsCancelled()
        {
            lock (this) { return _parent == null; }
        }

        /// <summary>
        /// The interval between replications.
        /// </summary>
        /// <returns></returns>
        long TimeScheduler.Task.GetNextInterval()
        {
            return _period;
        }

        /// <summary>
        /// Do the replication.
        /// </summary>
        void TimeScheduler.Task.Run()
        {
            if (_parent != null)
            {
                _parent.UpdateStatistics();
            }
        }
    }
}