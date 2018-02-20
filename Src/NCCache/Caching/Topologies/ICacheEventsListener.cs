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

using Alachisoft.NCache.Common.DataStructures;
using Alachisoft.NCache.Caching.Queries;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching.Topologies
{
	/// <summary>
	/// Events callback interface used by the listeners of Cache events. 
	/// </summary>
	public interface ICacheEventsListener
	{
		/// <summary>
		/// Fired when an item is added to the cache.
		/// </summary>
		/// <param name="key">key of the cache item</param>
        void OnItemAdded(object key, OperationContext operationContext, EventContext eventContext);

		/// <summary>
		/// Fired when an item is updated in the cache.
		/// </summary>
		/// <param name="key">key of the cache item</param>
        void OnItemUpdated(object key, OperationContext operationContext, EventContext eventContext);

		/// <summary>
		/// Fired when an item is removed from the cache.
		/// </summary>
		/// <param name="key">key of the cache item</param>
		/// <param name="val">item itself</param>
		/// <param name="reason">reason the item was removed</param>
        void OnItemRemoved(object key, object val, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext);

		/// <summary>
		/// Fired when one ar many items are removed from the cache.
		/// </summary>
		/// <param name="key">keys of the cache item</param>
		/// <param name="val">items itself</param>
		/// <param name="reason">reason the item was removed</param>
        void OnItemsRemoved(object[] keys, object[] vals, ItemRemoveReason reason, OperationContext operationContext, EventContext[] eventContext);

		/// <summary>
		/// Fire when the cache is cleared.
		/// </summary>
        void OnCacheCleared(OperationContext operationContext, EventContext eventContext);

        /// <summary>
        /// Fire and make user happy.
        /// </summary>
        void OnCustomEvent(object notifId, object data, OperationContext operationContext, EventContext eventContext);

        void OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext);

        void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext);
#if !CLIENT
        /// <summary>
        /// Fire when hasmap changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap);
#endif

        void OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, CallbackEntry cbEntry);

        void OnActiveQueryChanged(object key, QueryChangeType changeType, List<CQCallbackInfo> activeQueries, OperationContext operationContext, EventContext eventContext);

        //MapReduce Callback
        void OnTaskCallback(string taskId, object value, OperationContext operationContext, EventContext eventContext);
        //

        void OnPollNotify(string clientId, short callbackId, Runtime.Events.EventType eventType);

    }
}
