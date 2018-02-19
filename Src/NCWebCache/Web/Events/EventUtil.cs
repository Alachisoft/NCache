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
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web
{
    internal static class EventUtil
    {
        internal static Alachisoft.NCache.Caching.EventId ConvertToEventID(Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem, NCache.Persistence.EventType eventType)
        {
            Alachisoft.NCache.Caching.EventId eventId = eventId = new Alachisoft.NCache.Caching.EventId();

            switch (eventType)
            {
                case NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK:
                    eventId.EventUniqueID = eventItem.ItemUpdatedCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.ItemUpdatedCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.ItemUpdatedCallback.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;
                    break;


                case NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK:
                    eventId.EventUniqueID = eventItem.itemRemoveCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemRemoveCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemRemoveCallback.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;
                    break;

            }
            return eventId;
        }
        
        internal static EventCacheItem ConvertToEventEntry(Alachisoft.NCache.Common.Protobuf.EventCacheItem cacheItem)
        {
            byte[] objectValue = null;

            if (cacheItem == null) return null;
            
            EventCacheItem item = new EventCacheItem();
            item.CacheItemPriority = (CacheItemPriority)cacheItem.priority;
            if (cacheItem.value != null && cacheItem.value.Count > 0)
            {
                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(cacheItem.value.ToArray());
                objectValue = ubObject.GetFullObject();
                item.Value = objectValue;
            }

            return item;

        }

        /// <summary>
        /// For Inproc only
        /// </summary>
        /// <param name="entry"></param>
        /// <returns></returns>
        internal static EventCacheItem ConvertToItem(EventCacheEntry entry)
        {
            Byte[] objectValue = null;
            if (entry != null)
            {
                EventCacheItem item = new EventCacheItem();
                item.CacheItemPriority = (CacheItemPriority)entry.Priority;
                if (entry.Value != null)
                {
                    UserBinaryObject ubObject = entry.Value as UserBinaryObject;
                    if (ubObject != null)
                    {
                        objectValue = ubObject.GetFullObject();
                        item.Value = objectValue;
                    }
                    else
                        item.Value = entry.Value;
                }


                return item;
            }

            return null;
        }

        internal static CacheItemRemovedReason ConvertToCIRemoveReason(ItemRemoveReason reason)
        {
            switch(reason)
            {
                case ItemRemoveReason.Expired:
                    return CacheItemRemovedReason.Expired;
                case ItemRemoveReason.Removed:
                    return CacheItemRemovedReason.Removed;
                case ItemRemoveReason.Underused:
                    return CacheItemRemovedReason.Underused;
                default:
                    return CacheItemRemovedReason.Underused;
            }
        }
    }
}
