// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// The periodic update task. Replicates cache stats to all nodes. _stats are used for
    /// load balancing as well as statistics reporting.
    /// </summary>
    class AutomaticDataLoadBalancer : TimeScheduler.Task
    {
        /// <summary> The parent on this task. </summary>
        private ClusterCacheBase _parent = null;

        /// <summary> The periodic interval for stat replications. </summary>
        private long _period = 5000;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent"></param>
        public AutomaticDataLoadBalancer(ClusterCacheBase parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// Overloaded Constructor.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="period"></param>
        public AutomaticDataLoadBalancer(ClusterCacheBase parent, long period)
        {
            _parent = parent;
            _period = period;
        }

        /// <summary>
        /// Sets the cancel flag.
        /// </summary>
        public void Cancel()
        {
            lock (this) { _parent = null; }
        }


        /// <summary>
        /// The task is cancelled or not. 
        /// </summary>
        /// <returns></returns>
        public override bool IsCancelled()
        {
            lock (this) { return _parent == null; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override long GetNextInterval()
        {
            return _period;
        }

        /// <summary>
        /// 
        /// </summary>
        public override void Run()
        {
            if (_parent != null)
            {
                try
                {
                    _parent.AutoLoadBalance();
                }
                catch (Exception e)
                {
                    _parent.Context.NCacheLog.Error("AutomaticLoadBalancer.Run", e.ToString());
                }
            }
        }
    }
}
