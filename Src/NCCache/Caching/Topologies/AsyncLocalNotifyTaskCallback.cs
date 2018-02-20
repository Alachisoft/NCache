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

using Alachisoft.NCache.Common.Threading;
using System;

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Asynchronous task notification dispatcher.
    /// </summary>
    internal class AsyncLocalNotifyTaskCallback : AsyncProcessor.IAsyncTask
    {

        /**
         * The listener class
         */
        private ICacheEventsListener _listener;
        /**
         * Message to broadcast
         */
        private string _taskID;
        private Object _entry;
        private OperationContext _operationContext;
        private EventContext _eventContext;

        /**
         * Constructor
         *
         * @param listener
         * @param data
         */
        public AsyncLocalNotifyTaskCallback(ICacheEventsListener listener, string taskID, Object entry, OperationContext operationContext, EventContext eventContext)
        {
            _listener = listener;
            _taskID = taskID;
            _entry = entry;
            _operationContext = operationContext;
            _eventContext = eventContext;
        }

        /**
         * Implementation of message sending.
         */

        public void Process()
        {
            _listener.OnTaskCallback(_taskID, _entry, _operationContext, _eventContext);
        }

    }

}