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
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Caching.Queries.Filters;
using System.Collections;
using Alachisoft.NCache.Caching.Topologies.Local;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Queries
{
    /// <summary>
    /// Observes the cache operations that may cause changes in some 
    /// of the registered active query result sets.
    /// </summary>

    internal interface IQueryOperationsObserver
    {
        void OnItemAdded(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext);
        void OnItemUpdated(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext);
        void OnItemRemoved(object key, MetaInformation metaInfo, LocalCacheBase cache, string cacheContext, bool notify, OperationContext operationContext, EventContext eventContext);        
    }
}
