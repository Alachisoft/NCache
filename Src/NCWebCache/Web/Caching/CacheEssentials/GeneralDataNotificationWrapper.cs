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

using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.Web.Caching
{
    internal class GeneralDataNotificationWrapper
    {
        private Cache _parentCache;

        public GeneralDataNotificationWrapper(Cache parentCache)
        {
            _parentCache = parentCache;
        }

        public void OnCacheDataNotification(string key, CacheEventArg eventArgs)
        {
            switch (eventArgs.EventType)
            {
                case EventType.ItemAdded:
                    _parentCache.EventListener.OnItemAdded(key, false);
                    break;

                case EventType.ItemUpdated:
                    _parentCache.EventListener.OnItemUpdated(key, false);
                    break;

                case EventType.ItemRemoved:
                    _parentCache.EventListener.OnItemRemoved(key, eventArgs.Item.Value,
                        eventArgs.CacheItemRemovedReason, false);
                    break;
            }
        }
    }
}
