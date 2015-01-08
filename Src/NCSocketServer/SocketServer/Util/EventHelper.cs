// Copyright (c) 2015 Alachisoft
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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Queries;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Events;

namespace Alachisoft.NCache.SocketServer.Util
{
    internal static class EventHelper
    {
        internal static ItemRemoveCallbackResponse GetItemRemovedCallbackResponse(EventContext eventContext, short id, string key, UserBinaryObject value, BitSet flag, ItemRemoveReason reason,EventDataFilter dataFilter)
        {
            Alachisoft.NCache.Common.Protobuf.ItemRemoveCallbackResponse itemRemovedCallback = new Alachisoft.NCache.Common.Protobuf.ItemRemoveCallbackResponse();

            itemRemovedCallback.key = key;
            itemRemovedCallback.callbackId = id;
            itemRemovedCallback.itemRemoveReason = (int)reason;
            itemRemovedCallback.flag = flag != null? (int)flag.Data : 0;
            itemRemovedCallback.dataFilter = (short)dataFilter;
            itemRemovedCallback.eventId = new Common.Protobuf.EventId();
            UserBinaryObject binaryObject = eventContext.Item != null ?(UserBinaryObject) eventContext.Item.Value : null;
            if(binaryObject != null) itemRemovedCallback.value.AddRange(binaryObject.DataList);

            if (eventContext != null)
            {
                itemRemovedCallback.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                itemRemovedCallback.eventId.operationCounter = eventContext.EventID.OperationCounter;
                itemRemovedCallback.eventId.eventCounter = eventContext.EventID.EventCounter;
                if (eventContext.Item != null)
                    itemRemovedCallback.flag = eventContext.Item.Flags.Data;
                itemRemovedCallback.eventId.item = ConvertToEventItem(eventContext.Item, null);
                if (itemRemovedCallback.eventId.item != null && itemRemovedCallback.eventId.item.value != null)
                {
                    itemRemovedCallback.eventId.item.value.Clear();//data will be travalling in old fashion due to old callbacks 
                }
            }

            return itemRemovedCallback;
        }

        internal static ItemUpdatedCallbackResponse GetItemUpdatedCallbackResponse(EventContext eventContext, string key, short callbackid, EventDataFilter dataFilter)
        {
            Alachisoft.NCache.Common.Protobuf.ItemUpdatedCallbackResponse itemUpdatedCallback = new Alachisoft.NCache.Common.Protobuf.ItemUpdatedCallbackResponse();

            itemUpdatedCallback.key = key;
            itemUpdatedCallback.callbackId = callbackid;
            itemUpdatedCallback.dataFilter = (short)dataFilter;
            itemUpdatedCallback.eventId = new Common.Protobuf.EventId();
            if (eventContext != null)
            {
                itemUpdatedCallback.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                itemUpdatedCallback.eventId.operationCounter = eventContext.EventID.OperationCounter;
                itemUpdatedCallback.eventId.eventCounter = eventContext.EventID.EventCounter;

                itemUpdatedCallback.eventId.item = ConvertToEventItem(eventContext.Item, dataFilter);
                itemUpdatedCallback.eventId.oldItem = ConvertToEventItem(eventContext.OldItem, dataFilter);
            }

            return itemUpdatedCallback;
        }

        internal static CacheStoppedEventResponse GetCacheStoppedEventResponse(string cacheId)
        {
            Alachisoft.NCache.Common.Protobuf.CacheStoppedEventResponse cacheStopped = new Alachisoft.NCache.Common.Protobuf.CacheStoppedEventResponse();
            cacheStopped.cacheId = cacheId;

            return cacheStopped;
        }

        internal static EventCacheItem ConvertToEventItem(EventCacheEntry entry, EventDataFilter? datafilter)
        {
            if (datafilter == EventDataFilter.None || entry == null)
                return null;

            EventCacheItem cacheItem = new EventCacheItem();

            cacheItem.priority = (int)entry.Priority;

            UserBinaryObject userBinary = entry.Value as UserBinaryObject;
            if (userBinary == null)
            {
                if (entry.Value is CallbackEntry)
                    userBinary = ((CallbackEntry)entry.Value).Value as UserBinaryObject;
            }

            if (userBinary != null)
                cacheItem.value.AddRange(userBinary.DataList);

            //Can be optimized
            if (datafilter != null)
            {
                if (datafilter == EventDataFilter.None)
                    return null;
                else if (datafilter == EventDataFilter.Metadata)
                    cacheItem.value.Clear();
            }

            return cacheItem;

        }
    }
}
