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

using System;
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
        
        void OnCustomUpdateCallback(object key, object value, OperationContext operationContext, EventContext eventContext);

        void OnCustomRemoveCallback(object key, object value, ItemRemoveReason reason, OperationContext operationContext, EventContext eventContext);

        /// <summary>
        /// Fire when hasmap changes when 
        /// - new node joins
        /// - node leaves
        /// - manual/automatic load balance
        /// </summary>
        /// <param name="newHashmap">new hashmap</param>
        void OnHashmapChanged(NewHashmap newHashmap, bool updateClientMap);
	}
}
