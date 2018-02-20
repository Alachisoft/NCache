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
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;
using System.Collections;

namespace Alachisoft.NCache.Caching.CacheSynchronization
{    

	public interface ISyncCache
    {
		object Get(string key, ref ulong version, ref BitSet flag,ref DateTime absoluteExpiration,ref TimeSpan slidingExpiration, ref long size, ref string group, ref string subGroup, ref Hashtable queryInfo);
        void RegisterSyncKeyNotifications(string key, ISyncCacheEventsListener eventListener, CallbackType callbackType);
        void UnRegisterSyncKeyNotifications(string key, ISyncCacheEventsListener eventListener);
        void RegisterBulkSyncKeyNotifications(string[] key, ISyncCacheEventsListener eventListener, CallbackType callbackType);
        void UnRegisterBulkSyncKeyNotifications(string[] key, ISyncCacheEventsListener eventListener);
      
        void Initialize();
        string CacheId { get; set; }
        bool IsModeInProc { get; }
        CallbackType GetNotificationType(string cacheId);
        void Dispose();
        void OnCacheClear();
 
    }
}
