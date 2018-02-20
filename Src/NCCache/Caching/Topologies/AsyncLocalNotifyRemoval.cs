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

namespace Alachisoft.NCache.Caching.Topologies
{
    /// <summary>
    /// Asynchronous notification dispatcher.
    /// </summary>
    internal class AsyncLocalNotifyRemoval : AsyncProcessor.IAsyncTask
    {
        /// <summary> The listener class </summary>
        private ICacheEventsListener _listener;

        /// <summary> Message to broadcast </summary>
        private object _key, _value;
        private ItemRemoveReason _reason = ItemRemoveReason.Removed;

        private OperationContext _operationContext;
        private object _eventContext;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="data"></param>
        public AsyncLocalNotifyRemoval(ICacheEventsListener listener, object key, object value, ItemRemoveReason reason, OperationContext operationContext, object eventContext)
        {
            _listener = listener;
            _key = key;
            _value = value;
            _reason = reason;
            _operationContext = operationContext;
            _eventContext = eventContext;
        }

        /// <summary>
        /// Implementation of message sending.
        /// </summary>

        void AsyncProcessor.IAsyncTask.Process()
        {
            //		object[] objs = (object[])_data;
            if (_key is object[])
            {
                _listener.OnItemsRemoved((object[])_key,
                    (object[])_value, _reason, _operationContext, (EventContext[])_eventContext);
            }
            else
            {
                _listener.OnItemRemoved(_key, _value, _reason, _operationContext, (EventContext)_eventContext);
            }
        }

    }
}
