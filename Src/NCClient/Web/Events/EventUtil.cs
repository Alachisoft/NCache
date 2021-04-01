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

using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Client.Caching;
using System;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Runtime.Caching;

namespace Alachisoft.NCache.Client
{
    internal static class EventUtil
    {
        internal static EventId ConvertToEventID(Common.Protobuf.BulkEventItemResponse eventItem, Alachisoft.NCache.Persistence.EventType eventType)
        {
            EventId eventId = eventId = new EventId();

            switch (eventType)
            {
                case Persistence.EventType.ITEM_ADDED_EVENT:
                    eventId.EventUniqueID = eventItem.itemAddedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemAddedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemAddedEvent.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.ITEM_ADDED_EVENT;
                    break;

                case Persistence.EventType.ITEM_UPDATED_EVENT:
                    eventId.EventUniqueID = eventItem.itemUpdatedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemUpdatedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemUpdatedEvent.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.ITEM_UPDATED_EVENT;
                    break;

                case Persistence.EventType.ITEM_UPDATED_CALLBACK:
                    eventId.EventUniqueID = eventItem.ItemUpdatedCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.ItemUpdatedCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.ItemUpdatedCallback.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.ITEM_UPDATED_CALLBACK;
                    break;



                case Persistence.EventType.ITEM_REMOVED_CALLBACK:
                    eventId.EventUniqueID = eventItem.itemRemoveCallback.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemRemoveCallback.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemRemoveCallback.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.ITEM_REMOVED_CALLBACK;
                    break;

                case Persistence.EventType.ITEM_REMOVED_EVENT:
                    eventId.EventUniqueID = eventItem.itemRemovedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.itemRemovedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.itemRemovedEvent.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.ITEM_REMOVED_EVENT;
                    break;

                case Persistence.EventType.CACHE_CLEARED_EVENT:
                    eventId.EventUniqueID = eventItem.cacheClearedEvent.eventId.eventUniqueId;
                    eventId.EventCounter = eventItem.cacheClearedEvent.eventId.eventCounter;
                    eventId.OperationCounter = eventItem.cacheClearedEvent.eventId.operationCounter;
                    eventId.EventType = Persistence.EventType.CACHE_CLEARED_EVENT;
                    break;

            }
            return eventId;
        }

        internal static EventCacheItem ConvertToEventEntry(Alachisoft.NCache.Common.Protobuf.EventCacheItem cacheItem)
        {
            Byte[] objectValue = null;

            if (cacheItem == null) return null;

            EventCacheItem item = new EventCacheItem();
            item.CacheItemPriority = (CacheItemPriority)cacheItem.priority;
            item.CacheItemVersion = new CacheItemVersion(cacheItem.itemVersion);
            item.Group = String.IsNullOrEmpty(cacheItem.group) ? null : cacheItem.group;
            item.SubGroup = String.IsNullOrEmpty(cacheItem.subGroup) ? null : cacheItem.subGroup;
            item.Flagmap = new Common.BitSet((byte)cacheItem.flagMap);
            if (cacheItem.value != null && cacheItem.value.Count > 0)
            {
                UserBinaryObject ubObject = UserBinaryObject.CreateUserBinaryObject(cacheItem.value.ToArray());
                objectValue = ubObject.GetFullObject();
                item.SetValue(objectValue);
            }

            item.EntryType = NCache.Util.MiscUtil.ProtoItemTypeToEntryType(cacheItem.itemType);

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
                item.CacheItemVersion = new CacheItemVersion(entry.Version);
                item.Group = entry.Group;
                item.SubGroup = entry.SubGroup;
                
                if (entry.Value != null)
                {
                    UserBinaryObject ubObject = entry.Value as UserBinaryObject;
                    if (ubObject != null)
                    {
                        objectValue = ubObject.GetFullObject();
                        item.SetValue(objectValue);
                    }
                    else
                        item.SetValue(entry.Value);
                }
                item.EntryType = entry.EntryType;

                return item;
            }

            return null;
        }

        internal static CacheItemRemovedReason ConvertToCIRemoveReason(ItemRemoveReason reason)
        {
            switch (reason)
            {
                case ItemRemoveReason.Expired:
                    return CacheItemRemovedReason.Expired;
                    break;
                case ItemRemoveReason.Removed:
                    return CacheItemRemovedReason.Removed;
                    break;
                case ItemRemoveReason.Underused:
                    return CacheItemRemovedReason.Underused;
                    break;
                default:
                    return CacheItemRemovedReason.Underused;
                    break;
            }
        }

        internal enum MessageItemType
        {
            MessageEventItem,
            MessageEventItems,
            MessageItems,
            MessageEventCollection,
            MessageEventCollections,
        }

        internal static MessageItemType GetMessageItemType(MessageItem messageItem)
        {
            if (messageItem != null)
            {
                if (messageItem.Payload != null)
                {
                    if (messageItem.Payload is MessageEventItem[])
                        return MessageItemType.MessageEventItems;
                    else if (messageItem.Payload is MessageEventItem)
                        return MessageItemType.MessageEventItem;
                
                    return MessageItemType.MessageItems;
                }
            }
            return MessageItemType.MessageItems;

        }

        internal static MessageItemType GetMessageItemType(MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs != null)
            {
                if (messageEventArgs.Message != null && messageEventArgs.Message.Payload != null)
                {
                    if (messageEventArgs.Message.Payload is MessageEventItem[])
                        return MessageItemType.MessageEventItems;
                    else if (messageEventArgs.Message.Payload is MessageEventItem)
                        return MessageItemType.MessageEventItem;
                    return MessageItemType.MessageItems;
                }
            }
            return MessageItemType.MessageItems;

        }

    }
}
