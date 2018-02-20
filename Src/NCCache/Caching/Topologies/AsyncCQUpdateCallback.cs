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

using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common.Threading;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    ///Asynchronous notification dispatcher. 
    /// </summary>
    internal class AsyncCQUpdateCallback : AsyncProcessor.IAsyncTask
    {
        /// <summary> The listener class </summary>
        private ICacheEventsListener _listener;

        /// <summary> Message to broadcast </summary> 
        private object _key;

        private QueryChangeType _changeType;

        private List<CQCallbackInfo> _queries;
        private OperationContext _operationContext;
        private EventContext _eventContext;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="data"></param>
        public AsyncCQUpdateCallback(ICacheEventsListener listener, object key, QueryChangeType changeType, List<CQCallbackInfo> queries, OperationContext operationContext, EventContext eventContext)
        {
            _listener = listener;
            _key = key;
            _changeType = changeType;
            _queries = queries;
            _operationContext = operationContext;
            _eventContext = eventContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>

        void AsyncProcessor.IAsyncTask.Process()
        {
            _listener.OnActiveQueryChanged(_key, _changeType, _queries, _operationContext, _eventContext);
        }

    }
}
