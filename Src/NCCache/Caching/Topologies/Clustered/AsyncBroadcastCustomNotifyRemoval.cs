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
using Alachisoft.NCache.Common.Pooling;
using Alachisoft.NCache.Common.Threading;

namespace Alachisoft.NCache.Caching.Topologies.Clustered
{
    /// <summary>
    /// Asynchronous notification dispatcher.
    /// </summary>
    internal class AsyncBroadcastCustomNotifyRemoval : AsyncProcessor.IAsyncTask
    {
        /// <summary> The listener class </summary>
        private ClusterCacheBase _parent;
        /// <summary> Message to broadcast </summary>
        private object _key;
        private ItemRemoveReason _reason = ItemRemoveReason.Removed;
        private OperationContext _operationContext;
        private EventContext _eventContext;
        private CacheEntry _entry;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="data"></param>
        public AsyncBroadcastCustomNotifyRemoval(ClusterCacheBase parent, object key, CacheEntry entry, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext)
        {
            _parent = parent;
            _key = key;
            _entry = entry;
            _reason = reason;
            _operationContext = operationContext;
            _eventContext = eventContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>
        void AsyncProcessor.IAsyncTask.Process()
        {
            try
            {
                if (_parent != null)
                {
                    if (_entry != null)
                        _entry.MarkInUse(NCModulesConstants.Asynctask);
                    _parent.RaiseCustomRemoveCalbackNotifier(_key, _entry, _reason, false, _operationContext, _eventContext);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}