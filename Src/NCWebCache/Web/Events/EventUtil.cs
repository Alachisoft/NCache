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
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Web.Caching;

namespace Alachisoft.NCache.Web
{
    internal static class EventUtil
    {
        internal static Alachisoft.NCache.Caching.EventId ConvertToEventID(
            Alachisoft.NCache.Common.Protobuf.BulkEventItemResponse eventItem, NCache.Persistence.EventType eventType)
        {
            Alachisoft.NCache.Caching.EventId eventId = eventId = new Alachisoft.NCache.Caching.EventId();

            switch (eventType)
            {
                case NCache.Persistence.EventType.ITEM_ADDED_EVENT:
                    eventId.EventUniqueID = eventItem.itemAddedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemAddedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemAddedEvent.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_ADDED_EVENT;
                    break;

                case NCache.Persistence.EventType.ITEM_UPDATED_EVENT:
                    eventId.EventUniqueID = eventItem.itemUpdatedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemUpdatedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemUpdatedEvent.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_EVENT;
                    break;

                case NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK:
                    eventId.EventUniqueID = eventItem.ItemUpdatedCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.ItemUpdatedCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.ItemUpdatedCallback.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_UPDATED_CALLBACK;
                    break;


                case NCache.Persistence.EventType.CQ_CALLBACK:
                    eventId.EventUniqueID = eventItem.CQCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.CQCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.CQCallback.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.CQ_CALLBACK;
                    eventId.QueryChangeType = (NCache.Caching.Queries.QueryChangeType) eventItem.CQCallback.changeType;
                    eventId.QueryId = eventItem.CQCallback.queryId;
                    break;


                case NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK:
                    eventId.EventUniqueID = eventItem.itemRemoveCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemRemoveCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemRemoveCallback.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_CALLBACK;
                    break;

                case NCache.Persistence.EventType.ITEM_REMOVED_EVENT:
                    eventId.EventUniqueID = eventItem.itemRemovedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemRemovedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemRemovedEvent.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.ITEM_REMOVED_EVENT;
                    break;

                case NCache.Persistence.EventType.CACHE_CLEARED_EVENT:
                    eventId.EventUniqueID = eventItem.cacheClearedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.cacheClearedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.cacheClearedEvent.eventId.operationCounter;
                    eventId.EventType = NCache.Persistence.EventType.CACHE_CLEARED_EVENT;
                    break;
            }

            return eventId;
        }

        internal static EventCacheItem ConvertToEventEntry(Alachisoft.NCache.Common.Protobuf.EventCacheItem cacheItem)
        {
            Byte[] objectValue = null;

            if (cacheItem == null) return null;

            EventCacheItem item = new EventCacheItem();
            item.CacheItemPriority = (CacheItemPriority) cacheItem.priority;
            item.CacheItemVersion = new Caching.CacheItemVersion(cacheItem.itemVersion);
            item.Group = String.IsNullOrEmpty(cacheItem.group) ? null : cacheItem.group;
            item.ResyncExpiredItems = cacheItem.resyncExpiredItems;
            item.ResyncProviderName =
                String.IsNullOrEmpty(cacheItem.resyncProviderName) ? null : cacheItem.resyncProviderName;
            item.SubGroup = String.IsNullOrEmpty(cacheItem.subGroup) ? null : cacheItem.subGroup;
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
                item.CacheItemPriority = (CacheItemPriority) entry.Priority;
                item.CacheItemVersion = new Caching.CacheItemVersion(entry.Version);
                item.Group = entry.Group;
                item.ResyncExpiredItems = entry.ReSyncExpiredItems;
                item.ResyncProviderName = entry.ReSyncProviderCacheItem;
                item.SubGroup = entry.SubGroup;
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
            switch (reason)
            {
                case ItemRemoveReason.DependencyChanged:
                    return CacheItemRemovedReason.DependencyChanged;
                case ItemRemoveReason.DependencyInvalid:
                    return CacheItemRemovedReason.DependencyChanged;
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