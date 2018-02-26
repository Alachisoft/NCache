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
using Alachisoft.NCache.Caching;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Caching.Queries;
using System.Collections;
using Alachisoft.NCache.Common;
using Alachisoft.NCache.Runtime.Caching;
using Alachisoft.NCache.Runtime.Events;
using Alachisoft.NCache.Common.Util;


namespace Alachisoft.NCache.SocketServer.Util
{
    internal static class EventHelper
    {
        internal static AsyncOperationCompletedCallbackResponse GetAsyncOpCompletedResponse(ClientManager clientManager, AsyncCallbackInfo cbInfo, object opResult, object opCode, string key)
        {
            Alachisoft.NCache.Common.Protobuf.AsyncOperationCompletedCallbackResponse asyncOperationCompleted = new Alachisoft.NCache.Common.Protobuf.AsyncOperationCompletedCallbackResponse();

            switch ((AsyncOpCode)opCode)
            {
                case AsyncOpCode.Add:
                case AsyncOpCode.Remove:
                case AsyncOpCode.Update:
                    asyncOperationCompleted.key = key;
                    break;
            }

            asyncOperationCompleted.requestId = cbInfo.RequestID;

            if (opResult is System.Exception)
            {
                Alachisoft.NCache.Common.Protobuf.Exception exc = new Alachisoft.NCache.Common.Protobuf.Exception();
                exc.message = ((System.Exception)opResult).Message;
                exc.exception = ((System.Exception)opResult).ToString();
                exc.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;

                asyncOperationCompleted.exc = exc;
                asyncOperationCompleted.success = false;
            }
            else
            {
                asyncOperationCompleted.success = true;
            }

            return asyncOperationCompleted;
        }

        internal static CQCallbackResponse GetCQCallbackResponse(EventContext eventContext, string key, string queryId, QueryChangeType changeType, EventDataFilter datafilter)
        {
            Alachisoft.NCache.Common.Protobuf.CQCallbackResponse CQCallbackResponse = new Alachisoft.NCache.Common.Protobuf.CQCallbackResponse();

            CQCallbackResponse.key = key;
            CQCallbackResponse.queryId = queryId;
            CQCallbackResponse.changeType = Convert.ToInt32(changeType);
            CQCallbackResponse.eventId = new Common.Protobuf.EventId();
            if (eventContext != null)
            {
                CQCallbackResponse.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                CQCallbackResponse.eventId.operationCounter = eventContext.EventID.OperationCounter;
                CQCallbackResponse.eventId.eventCounter = eventContext.EventID.EventCounter;

                CQCallbackResponse.eventId.item = ConvertToEventItem(eventContext.Item, datafilter);
                CQCallbackResponse.eventId.oldItem = ConvertToEventItem(eventContext.OldItem, datafilter);
                CQCallbackResponse.datafilter = (int)datafilter;
            }
            return CQCallbackResponse;
        }

        internal static TaskCallbackResponse GetTaskCallbackResponse(EventContext eventContext, string taskId, short taskStatus, string taskFailureReason, short callbackId)
        {
            TaskCallbackResponse resp = new TaskCallbackResponse();

            resp.CallbackId = callbackId;
            resp.TaskStatus = taskStatus;
            resp.TaskFailureReason = taskFailureReason;
            resp.TaskId = taskId;
            resp.TaskResult = null;

            Common.Protobuf.EventId eventId = new Common.Protobuf.EventId();

            if (eventContext != null && eventContext.EventID != null)
            {
                eventId.eventUniqueId =eventContext.EventID.EventUniqueID;
                eventId.operationCounter = eventContext.EventID.OperationCounter;
                eventId.eventCounter = eventContext.EventID.EventCounter;

            }
            resp.EventId = eventId;

            return resp;
        }

        internal static DSUpdatedCallbackResponse GetDSUPdateCallbackResponse(short id, OpCode opCode, object result)
        {
            Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResponse dsUpdatedCallback = new Alachisoft.NCache.Common.Protobuf.DSUpdatedCallbackResponse();

            dsUpdatedCallback.callbackId = id;
            dsUpdatedCallback.opCode = (int)opCode;
            KeyPackageBuilder.PackageMisc(result as Hashtable, dsUpdatedCallback.result);

            return dsUpdatedCallback;
        }

        internal static ItemRemoveCallbackResponse GetItemRemovedCallbackResponse(EventContext eventContext, short id, string key, UserBinaryObject value, BitSet flag, ItemRemoveReason reason,EventDataFilter dataFilter)
        {
            Alachisoft.NCache.Common.Protobuf.ItemRemoveCallbackResponse itemRemovedCallback = new Alachisoft.NCache.Common.Protobuf.ItemRemoveCallbackResponse();

            itemRemovedCallback.key = key;
            itemRemovedCallback.callbackId = id;
            itemRemovedCallback.itemRemoveReason = (int)reason;
            itemRemovedCallback.flag = flag != null? (int)flag.Data : 0;
            itemRemovedCallback.dataFilter = (short)dataFilter;
            itemRemovedCallback.eventId = new Common.Protobuf.EventId();

            UserBinaryObject binaryObject = eventContext.Item != null ? eventContext.Item.Value != null ? getData(eventContext.Item) : null : null;
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
                    itemRemovedCallback.eventId.item.value.Clear(); // data will be travalling in old fashion due to old callbacks 
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

        internal static CustomEventResponse GetCustomEventResponse(byte[] key, byte[] value)
        {
            Alachisoft.NCache.Common.Protobuf.CustomEventResponse customeEventRespone = new Alachisoft.NCache.Common.Protobuf.CustomEventResponse();

            customeEventRespone.key = key;
            customeEventRespone.value = value;

            return customeEventRespone;
        }

        internal static ItemAddedEventResponse GetItemAddedEventResponse(EventContext eventContext, string key, EventDataFilter? datafilter)
        {
            Alachisoft.NCache.Common.Protobuf.ItemAddedEventResponse itemAdded = new Alachisoft.NCache.Common.Protobuf.ItemAddedEventResponse();
            itemAdded.key = key;

            itemAdded.eventId = new Common.Protobuf.EventId();
            if (eventContext != null)
            {
                itemAdded.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                itemAdded.eventId.operationCounter = eventContext.EventID.OperationCounter;
                itemAdded.eventId.eventCounter = eventContext.EventID.EventCounter;

                if (eventContext != null && eventContext.Item != null && eventContext.Item.Flags != null)
                {
                    itemAdded.flag = eventContext.Item.Flags.Data; 
                }

                itemAdded.eventId.item = ConvertToEventItem(eventContext.Item, datafilter);
            }
            return itemAdded;
        }

        internal static ItemRemovedEventResponse GetItemRemovedEventResponse(EventContext eventContext, string key, EventDataFilter? datafilter, BitSet flag, ItemRemoveReason reason, UserBinaryObject value)
        {
            Alachisoft.NCache.Common.Protobuf.ItemRemovedEventResponse itemRemoved = new Alachisoft.NCache.Common.Protobuf.ItemRemovedEventResponse();

            itemRemoved.key = key;
            
            itemRemoved.itemRemoveReason = (int)reason;

            // Value sent seperately to support old clients
            if (eventContext.Item != null && eventContext.Item.Value != null)
            {
                itemRemoved.flag = eventContext.Item.Flags.Data;
                UserBinaryObject ubObject = getData(eventContext.Item);
                if(ubObject!=null)
                    itemRemoved.value.AddRange(ubObject.DataList);
            }
            
            itemRemoved.eventId = new Common.Protobuf.EventId();
            if (eventContext != null)
            {
                itemRemoved.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                itemRemoved.eventId.operationCounter = eventContext.EventID.OperationCounter;
                itemRemoved.eventId.eventCounter = eventContext.EventID.EventCounter;

                itemRemoved.eventId.item = ConvertToEventItem(eventContext.Item, datafilter);

                if (itemRemoved.eventId.item != null && itemRemoved.eventId.item.value != null)
                {
                    // Value sent seperately to support old clients
                    itemRemoved.eventId.item.value.Clear(); 
                }
            }
            return itemRemoved;
        }

        internal static ClientConnectivityChangeEventResponse GetClientConnectivityChangeEventResponse(string cacheId,
            Runtime.Caching.ClientInfo client, ConnectivityStatus status)
        {
            ClientConnectivityChangeEventResponse response = new ClientConnectivityChangeEventResponse();
            response.cacheId = cacheId;
            response.status = (int) status;
            response.clientInfo = new Common.Protobuf.ClientInfo();

            response.clientInfo.appName = client.AppName;
            response.clientInfo.machineName = client.MachineName;
            response.clientInfo.ipAddress = client.IPAddress.ToString();
            response.clientInfo.clientId = client.ClientID;
            response.clientInfo.processId = client.ProcessID;

            return response;
        }

        internal static ItemUpdatedEventResponse GetItemUpdatedEventResponse(EventContext eventContext, string key, EventDataFilter? datafilter)
        {
            Alachisoft.NCache.Common.Protobuf.ItemUpdatedEventResponse itemUpdated = new Alachisoft.NCache.Common.Protobuf.ItemUpdatedEventResponse();

            itemUpdated.key = key;
            itemUpdated.eventId = new Common.Protobuf.EventId();

            if (eventContext != null)
            {
                itemUpdated.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                itemUpdated.eventId.operationCounter = eventContext.EventID.OperationCounter;
                itemUpdated.eventId.eventCounter = eventContext.EventID.EventCounter;

                if (eventContext != null && eventContext.Item != null && eventContext.Item.Flags != null)
                {
                    itemUpdated.flag = eventContext.Item.Flags.Data;
                }

                itemUpdated.eventId.oldItem = ConvertToEventItem(eventContext.OldItem, datafilter);
                itemUpdated.eventId.item = ConvertToEventItem(eventContext.Item, datafilter);
            }
            return itemUpdated;
        }

        internal static CacheClearedEventResponse GetCacheClearedResponse(EventContext eventContext)
        {
            Alachisoft.NCache.Common.Protobuf.CacheClearedEventResponse cacheCleared = new Alachisoft.NCache.Common.Protobuf.CacheClearedEventResponse();
            cacheCleared.eventId = new Common.Protobuf.EventId();
            if (eventContext != null)
            {
                cacheCleared.eventId.eventUniqueId = eventContext.EventID.EventUniqueID;
                cacheCleared.eventId.operationCounter = eventContext.EventID.OperationCounter;
                cacheCleared.eventId.eventCounter = eventContext.EventID.EventCounter;
            }
            return cacheCleared;
        }

        internal static PollNotifyEventResponse GetPollNotifyEvent(int callbackId, EventType eventType)
        {
            PollNotifyEventResponse response = new PollNotifyEventResponse();
            response.callbackId = callbackId;
            response.eventType = (int)eventType;
            return response;
        }

        internal static EventCacheItem ConvertToEventItem(EventCacheEntry entry, EventDataFilter? datafilter)
        {
            if (datafilter == EventDataFilter.None || entry == null)
                return null;

            EventCacheItem cacheItem = new EventCacheItem();
            cacheItem.group = entry.Group;
            cacheItem.subGroup = entry.SubGroup;

            cacheItem.itemVersion = entry.Version;
            cacheItem.priority = (int)entry.Priority;
            cacheItem.resyncExpiredItems = entry.ReSyncExpiredItems;
            cacheItem.resyncProviderName = entry.ReSyncProviderCacheItem;

            UserBinaryObject userBinary = getData(entry);
            if (userBinary != null)
                cacheItem.value.AddRange(userBinary.DataList);

            // Can be optimized
            if (datafilter != null)
            {
                if (datafilter == EventDataFilter.None)
                    return null;
                else if (datafilter == EventDataFilter.Metadata)
                    cacheItem.value.Clear();
            }
            return cacheItem;
        }

        private static IDataFormatService _eventDataFormat;

        internal static IDataFormatService EventDataFormat
        {
            set { _eventDataFormat = value; }
        }

        private static UserBinaryObject getData(EventCacheEntry entry)
        {
            object userValue = entry.Value;
            if (userValue is CallbackEntry)
            {
                userValue = ((CallbackEntry)userValue).Value;
            }
            if (userValue is UserBinaryObject)
            {
                userValue = ((UserBinaryObject)userValue).GetFullObject();
            }
            BitSet flag = entry.Flags;

            object ubObj = _eventDataFormat.GetClientData(userValue, ref flag, LanguageContext.DOTNET);
            return ubObj is UserBinaryObject ? (UserBinaryObject)ubObj : (UserBinaryObject)entry.Value;
        }
    }
}
