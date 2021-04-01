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
using Alachisoft.NCache.Common.DataStructures;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Enum;

namespace Alachisoft.NCache.Caching.Topologies
{
	/// <summary>
	/// Events callback interface used by the listeners of Cache events. 
	/// </summary>
	internal interface ICacheEventsListener
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

        #if !CLIENT && !DEVELOPMENT
        /// <summary>
        /// Fire when hasmap changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap);
        /// <summary>
        /// Fire when operation mode changes when 
        /// </summary>
        #endif

        void OnOperationModeChanged(OperationMode mode);

        void OnWriteBehindOperationCompletedCallback(OpCode operationCode, object result, Notifications notification);

        void OnPollNotify(string clientId, short callbackId, Caching.Events.EventTypeInternal eventType);
    }
}
