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
using System.Text;
using System.IO;
using Alachisoft.NCache.Common.DataStructures.Clustered;
using Alachisoft.NCache.Common.Protobuf;
using Alachisoft.NCache.Common.Protobuf.Util;
using Exception = System.Exception;
using Alachisoft.NCache.Runtime.Exceptions;
using System.Collections;

namespace Alachisoft.NCache.Common.Util
{
    public class ResponseHelper
    {
        public static Response DeserializeResponse(byte[] bytes)
        {
            Response response = null;
            byte[] length = new byte[10];
            Array.Copy(bytes, 0, length, 0, length.Length);
            int size = Convert.ToInt32(UTF8Encoding.UTF8.GetString(length));
            byte[] responseBytes = new byte[size];
            Array.Copy(bytes, 10, responseBytes, 0, size);
            using (MemoryStream ms = new MemoryStream(responseBytes))
            {
                response = Serializer.Deserialize<Response>(ms);
            }
            return response;
        }

        public static Response DeserializeResponse(Response.Type responseType, Stream stream)
        {
            Response response = null;

            switch (responseType)
            {
                case Response.Type.INSERT:
                    InsertResponse insertResponse = Serializer.Deserialize<InsertResponse>(stream);
                    response = new Response()
                    {
                        requestId = insertResponse.requestId,
                        commandID = insertResponse.commandID,
                        responseType = Response.Type.INSERT,
                        insert = insertResponse,
                    };
                    break;
                case Response.Type.GET:
                    GetResponse getResponse = Serializer.Deserialize<GetResponse>(stream);
                    response = new Response()
                    {
                        requestId = getResponse.requestId,
                        commandID = getResponse.commandID,
                        responseType = Response.Type.GET,
                        get = getResponse,
                    };
                    break;
                case Response.Type.INSERT_BULK:
                    BulkInsertResponse bulkInsertResponse = Serializer.Deserialize<BulkInsertResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkInsertResponse.requestId,
                        commandID = bulkInsertResponse.commandID,
                        intendedRecipient = bulkInsertResponse.intendedRecipient,
                        responseType = Response.Type.INSERT_BULK,
                        bulkInsert = bulkInsertResponse,
                    };
                    break;
                case Response.Type.GET_BULK:
                    BulkGetResponse bulkGetResponse = Serializer.Deserialize<BulkGetResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkGetResponse.requestId,
                        commandID = bulkGetResponse.commandID,
                        intendedRecipient = bulkGetResponse.intendedRecipient,
                        numberOfChuncks = bulkGetResponse.numberOfChuncks,
                        sequenceId = bulkGetResponse.sequenceId,
                        responseType = Response.Type.GET_BULK,
                        bulkGet = bulkGetResponse,
                    };
                    break;
                case Response.Type.EXECUTE_READER:
                    ExecuteReaderResponse executeReaderResponse = Serializer.Deserialize<ExecuteReaderResponse>(stream);
                    response = new Response()
                    {
                        requestId = executeReaderResponse.requestId,
                        commandID = executeReaderResponse.commandID,
                        numberOfChuncks = executeReaderResponse.numberOfChuncks,
                        sequenceId = executeReaderResponse.sequenceId,
                        responseType = Response.Type.EXECUTE_READER,
                        executeReaderResponse = executeReaderResponse,
                    };
                    break;
                case Response.Type.GET_READER_CHUNK:
                    GetReaderChunkResponse getreaderChunkResponse = Serializer.Deserialize<GetReaderChunkResponse>(stream);
                    response = new Response()
                    {
                        requestId = getreaderChunkResponse.requestId,
                        commandID = getreaderChunkResponse.commandID,
                        numberOfChuncks = getreaderChunkResponse.numberOfChuncks,
                        sequenceId = getreaderChunkResponse.sequenceId,
                        responseType = Response.Type.GET_READER_CHUNK,
                        getReaderChunkResponse = getreaderChunkResponse,
                    };
                    break;
                case Response.Type.GET_NEXT_CHUNK:
                    GetNextChunkResponse getNextChunkResponse = Serializer.Deserialize<GetNextChunkResponse>(stream);
                    response = new Response()
                    {
                        requestId = getNextChunkResponse.requestId,
                        commandID = getNextChunkResponse.commandID,
                        intendedRecipient = getNextChunkResponse.intendedRecipient,
                        responseType = Response.Type.GET_NEXT_CHUNK,
                        getNextChunkResponse = getNextChunkResponse,
                    };
                    break;

                case Response.Type.DELETE:
                    DeleteResponse deleteResponse = Serializer.Deserialize<DeleteResponse>(stream);
                    response = new Response()
                    {
                        requestId = deleteResponse.requestId,
                        commandID = deleteResponse.commandID,
                        responseType = Response.Type.DELETE,
                        deleteResponse = deleteResponse,
                    };
                    break;

                case Response.Type.REMOVE:
                    RemoveResponse removeResponse = Serializer.Deserialize<RemoveResponse>(stream);
                    response = new Response()
                    {
                        requestId = removeResponse.requestId,
                        commandID = removeResponse.commandID,
                        responseType = Response.Type.REMOVE,
                        remove = removeResponse
                    };
                    break;
                case Response.Type.REMOVE_GROUP:
                    RemoveGroupResponse removeGroupResponse = Serializer.Deserialize<RemoveGroupResponse>(stream);
                    response = new Response()
                    {
                        requestId = removeGroupResponse.requestId,
                        commandID = removeGroupResponse.commandID,
                        responseType = Response.Type.REMOVE_GROUP,
                        removeGroupResponse = removeGroupResponse
                    };
                    break;

                case Response.Type.COUNT:
                    CountResponse countResponse = Serializer.Deserialize<CountResponse>(stream);
                    response = new Response()
                    {
                        requestId = countResponse.requestId,
                        commandID = countResponse.commandID,
                        responseType = Response.Type.COUNT,
                        count = countResponse
                    };
                    break;

                case Response.Type.ADD:
                    AddResponse addResponse = Serializer.Deserialize<AddResponse>(stream);
                    response = new Response()
                    {
                        requestId = addResponse.requestId,
                        commandID = addResponse.commandID,
                        responseType = Response.Type.ADD,
                        addResponse = addResponse,
                    };
                    break;

                case Response.Type.ADD_BULK:
                    BulkAddResponse bulkAddResponse = Serializer.Deserialize<BulkAddResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkAddResponse.requestId,
                        commandID = bulkAddResponse.commandID,
                        intendedRecipient = bulkAddResponse.intendedRecipient,
                        responseType = Response.Type.ADD_BULK,
                        bulkAdd = bulkAddResponse,
                    };
                    break;

                case Response.Type.CLEAR:
                    ClearResponse clearResponse = Serializer.Deserialize<ClearResponse>(stream);
                    response = new Response()
                    {
                        requestId = clearResponse.requestId,
                        commandID = clearResponse.commandID,
                        responseType = Response.Type.CLEAR,
                        clearResponse = clearResponse
                    };
                    break;
                case Response.Type.CONTAINS_BULK:
                    ContainBulkResponse containBulkResponse = Serializer.Deserialize<ContainBulkResponse>(stream);
                    response = new Response()
                    {
                        requestId = containBulkResponse.requestId,
                        commandID = containBulkResponse.commandID,
                        responseType = Response.Type.CONTAINS_BULK,
                        containBulkResponse = containBulkResponse
                    };
                    break;
                case Response.Type.ADD_ATTRIBUTE:
                    AddAttributeResponse addAttribute = Serializer.Deserialize<AddAttributeResponse>(stream);
                    response = new Response()
                    {
                        requestId = addAttribute.requestId,
                        commandID = addAttribute.commandID,
                        responseType = Response.Type.ADD_ATTRIBUTE,
                        addAttributeResponse = addAttribute
                    };
                    break;

                case Response.Type.GET_CACHE_ITEM:
                    GetCacheItemResponse getCacheItem = Serializer.Deserialize<GetCacheItemResponse>(stream);
                    response = new Response()
                    {
                        requestId = getCacheItem.requestId,
                        commandID = getCacheItem.commandID,
                        responseType = Response.Type.GET_CACHE_ITEM,
                        getItem = getCacheItem,
                    };
                    break;
                
                case Response.Type.GET_GROUP_NEXT_CHUNK:
                    GetGroupNextChunkResponse getGroupNext = Serializer.Deserialize<GetGroupNextChunkResponse>(stream);
                    response = new Response()
                    {
                        requestId = getGroupNext.requestId,
                        commandID = getGroupNext.commandID,
                        responseType = Response.Type.GET_GROUP_NEXT_CHUNK,
                        getGroupNextChunkResponse = getGroupNext,
                    };
                    break;
                case Response.Type.BULK_GET_CACHEITEM:
                    BulkGetCacheItemResponse bulkGetCacheItem = Serializer.Deserialize<BulkGetCacheItemResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkGetCacheItem.requestId,
                        commandID = bulkGetCacheItem.commandID,
                        responseType = Response.Type.BULK_GET_CACHEITEM,
                        bulkGetCacheItem = bulkGetCacheItem,
                    };
                    break;
                case Response.Type.REMOVE_BULK:
                    BulkRemoveResponse bulkRemoveResponse = Serializer.Deserialize<BulkRemoveResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkRemoveResponse.requestId,
                        commandID = bulkRemoveResponse.commandID,
                        numberOfChuncks = bulkRemoveResponse.numberOfChuncks,
                        sequenceId = bulkRemoveResponse.sequenceId,
                        responseType = Response.Type.REMOVE_BULK,
                        bulkRemove = bulkRemoveResponse
                    };
                    break;
                case Response.Type.DELETE_BULK:
                    BulkDeleteResponse bulkDeleteResponse = Serializer.Deserialize<BulkDeleteResponse>(stream);
                    response = new Response()
                    {
                        requestId = bulkDeleteResponse.requestId,
                        commandID = bulkDeleteResponse.commandID,
                        intendedRecipient = bulkDeleteResponse.intendedRecipient,
                        responseType = Response.Type.DELETE_BULK,
                        bulkDeleteResponse = bulkDeleteResponse
                    };
                    break;
                case Response.Type.REMOVE_TOPIC:
                    RemoveTopicResponse removeTopicResponse = Serializer.Deserialize<RemoveTopicResponse>(stream);
                    response = new Response()
                    {
                        requestId = removeTopicResponse.requestId,
                        commandID = removeTopicResponse.commandID,
                        responseType = Response.Type.REMOVE_TOPIC,
                        removeTopicResponse = removeTopicResponse
                    };
                    break;
                case Response.Type.GET_TOPIC:
                    GetTopicResponse getTopic = Serializer.Deserialize<GetTopicResponse>(stream);
                    response = new Response()
                    {
                        requestId = getTopic.requestId,
                        commandID = getTopic.commandID,
                        responseType = Response.Type.GET_TOPIC,
                        getTopicResponse = getTopic
                    };
                    break;
                case Response.Type.GET_MESSAGE:
                    GetMessageResponse getMessage = Serializer.Deserialize<GetMessageResponse>(stream);
                    response = new Response()
                    {
                        requestId = getMessage.requestId,
                        commandID = getMessage.commandID,
                        responseType = Response.Type.GET_MESSAGE,
                        getMessageResponse = getMessage
                    };
                    break;
                case Response.Type.MESSAGE_PUBLISH:
                    MessagePublishResponse messagePublish = Serializer.Deserialize<MessagePublishResponse>(stream);
                    response = new Response()
                    {
                        requestId = messagePublish.requestId,
                        commandID = messagePublish.commandID,
                        responseType = Response.Type.MESSAGE_PUBLISH,
                        messagePublishResponse = messagePublish
                    };
                    break;
                case Response.Type.MESSAGE_COUNT:
                    MessageCountResponse messageCount = Serializer.Deserialize<MessageCountResponse>(stream);
                    response = new Response()
                    {
                        requestId = messageCount.requestId,
                        commandID = messageCount.commandID,
                        responseType = Response.Type.MESSAGE_COUNT,
                        messageCountResponse = messageCount
                    };
                    break;
               
                case Response.Type.DISPOSE:
                    DisposeResponse disposeResponse = Serializer.Deserialize<DisposeResponse>(stream);
                    response = new Response()
                    {
                        requestId = disposeResponse.requestId,
                        commandID = disposeResponse.commandID,
                        responseType = Response.Type.DISPOSE,
                        disposeResponse = disposeResponse
                    };
                    break;
                case Response.Type.DISPOSE_READER:
                    DisposeReaderResponse disposeReader = Serializer.Deserialize<DisposeReaderResponse>(stream);
                    response = new Response()
                    {
                        requestId = disposeReader.requestId,
                        commandID = disposeReader.commandID,
                        responseType = Response.Type.DISPOSE_READER,
                        disposeReaderResponse = disposeReader
                    };
                    break;
            
                case Response.Type.LOCK:
                    LockResponse lockResponse = Serializer.Deserialize<LockResponse>(stream);
                    response = new Response()
                    {
                        requestId = lockResponse.requestId,
                        commandID = lockResponse.commandID,
                        responseType = Response.Type.LOCK,
                        lockResponse = lockResponse
                    };
                    break;
                case Response.Type.UNLOCK:
                    UnlockResponse unlockResponse = Serializer.Deserialize<UnlockResponse>(stream);
                    response = new Response()
                    {
                        requestId = unlockResponse.requestId,
                        commandID = unlockResponse.commandID,
                        responseType = Response.Type.UNLOCK,
                        unlockResponse = unlockResponse
                    };
                    break;
                case Response.Type.ISLOCKED:
                    IsLockedResponse isLockedResponse = Serializer.Deserialize<IsLockedResponse>(stream);
                    response = new Response()
                    {
                        requestId = isLockedResponse.requestId,
                        commandID = isLockedResponse.commandID,
                        responseType = Response.Type.ISLOCKED,
                        isLockedResponse = isLockedResponse
                    };
                    break;
                case Response.Type.LOCK_VERIFY:
                    VerifyLockResponse verifyLockResponse = Serializer.Deserialize<VerifyLockResponse>(stream);
                    response = new Response()
                    {
                        requestId = verifyLockResponse.requestId,
                        commandID = verifyLockResponse.commandID,
                        responseType = Response.Type.LOCK_VERIFY,
                        lockVerify = verifyLockResponse
                    };
                    break;
                case Response.Type.REGISTER_KEY_NOTIF:
                    RegisterKeyNotifResponse registerKeyNotif = Serializer.Deserialize<RegisterKeyNotifResponse>(stream);
                    response = new Response()
                    {
                        requestId = registerKeyNotif.requestId,
                        commandID = registerKeyNotif.commandID,
                        responseType = Response.Type.REGISTER_KEY_NOTIF,
                        registerKeyNotifResponse = registerKeyNotif
                    };
                    break;
                case Response.Type.UNREGISTER_KEY_NOTIF:
                    UnregisterKeyNotifResponse unregisterKey = Serializer.Deserialize<UnregisterKeyNotifResponse>(stream);
                    response = new Response()
                    {
                        requestId = unregisterKey.requestId,
                        commandID = unregisterKey.commandID,
                        responseType = Response.Type.UNREGISTER_KEY_NOTIF,
                        unregisterKeyNotifResponse = unregisterKey
                    };
                    break;
                case Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                    UnregisterBulkKeyNotifResponse unregisterBulk = Serializer.Deserialize<UnregisterBulkKeyNotifResponse>(stream);
                    response = new Response()
                    {
                        requestId = unregisterBulk.requestId,
                        commandID = unregisterBulk.commandID,
                        responseType = Response.Type.UNREGISTER_BULK_KEY_NOTIF,
                        unregisterBulkKeyNotifResponse = unregisterBulk
                    };
                    break;
                case Response.Type.REGISTER_BULK_KEY_NOTIF:
                    RegisterBulkKeyNotifResponse registerBulkKey = Serializer.Deserialize<RegisterBulkKeyNotifResponse>(stream);
                    response = new Response()
                    {
                        requestId = registerBulkKey.requestId,
                        commandID = registerBulkKey.commandID,
                        responseType = Response.Type.REGISTER_BULK_KEY_NOTIF,
                        registerBulkKeyNotifResponse = registerBulkKey
                    };
                    break;
                case Response.Type.SUBSCRIBE_TOPIC:
                    SubscribeTopicResponse subscribeTopic = Serializer.Deserialize<SubscribeTopicResponse>(stream);
                    response = new Response()
                    {
                        requestId = subscribeTopic.requestId,
                        commandID = subscribeTopic.commandID,
                        responseType = Response.Type.SUBSCRIBE_TOPIC,
                        subscribeTopicResponse = subscribeTopic
                    };
                    break;
                case Response.Type.UNSUBSCRIBE_TOPIC:
                    UnSubscribeTopicResponse unSubscribeTopic = Serializer.Deserialize<UnSubscribeTopicResponse>(stream);
                    response = new Response()
                    {
                        requestId = unSubscribeTopic.requestId,
                        commandID = unSubscribeTopic.commandID,
                        responseType = Response.Type.UNSUBSCRIBE_TOPIC,
                        unSubscribeTopicResponse = unSubscribeTopic
                    };
                    break;
                case Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                    MessageAcknowledgmentResponse messageAcknowledgment = Serializer.Deserialize<MessageAcknowledgmentResponse>(stream);
                    response = new Response()
                    {
                        requestId = messageAcknowledgment.requestId,
                        commandID = messageAcknowledgment.commandID,
                        responseType = Response.Type.MESSAGE_ACKNOWLEDGEMENT,
                        messageAcknowledgmentResponse = messageAcknowledgment
                    };
                    break;
                case Response.Type.RAISE_CUSTOM_EVENT:
                    RaiseCustomEventResponse raiseCustomEvent = Serializer.Deserialize<RaiseCustomEventResponse>(stream);
                    response = new Response()
                    {
                        requestId = raiseCustomEvent.requestId,
                        commandID = raiseCustomEvent.commandID,
                        responseType = Response.Type.RAISE_CUSTOM_EVENT,
                        raiseCustomEventResponse = raiseCustomEvent
                    };
                    break;
                case Response.Type.GET_PRODUCT_VERSION:
                    GetProductVersionResponse getProductVersion = Serializer.Deserialize<GetProductVersionResponse>(stream);
                    response = new Response()
                    {
                        requestId = getProductVersion.requestId,
                        commandID = getProductVersion.commandID,
                        responseType = Response.Type.GET_PRODUCT_VERSION,
                        getProductVersionResponse = getProductVersion


                    };
                    break;
              
                case Response.Type.TOUCH:
                    TouchResponse touch = Serializer.Deserialize<TouchResponse>(stream);
                    response = new Response()
                    {
                        requestId = touch.requestId,
                        commandID = touch.commandID,
                        responseType = Response.Type.TOUCH,
                        touchResponse = touch
                    };
                    break;
                case Response.Type.POLL:
                    PollResponse poll = Serializer.Deserialize<PollResponse>(stream);
                    response = new Response()
                    {
                        requestId = poll.requestId,
                        commandID = poll.commandID,
                        responseType = Response.Type.POLL,
                        pollResponse = poll
                    };
                    break;
                case Response.Type.PING:
                    PingResponse ping = Serializer.Deserialize<PingResponse>(stream);
                    response = new Response()
                    {
                        requestId = ping.requestId,
                        commandID = ping.commandID,
                        responseType = Response.Type.PING,
                        pingResponse = ping
                    };
                    break;
                case Response.Type.REGISTER_POLL_NOTIF:
                    RegisterPollNotifResponse registerPollNotif = Serializer.Deserialize<RegisterPollNotifResponse>(stream);
                    response = new Response()
                    {
                        requestId = registerPollNotif.requestId,
                        commandID = registerPollNotif.commandID,
                        responseType = Response.Type.REGISTER_POLL_NOTIF,
                        registerPollNotifResponse = registerPollNotif,
                    };
                    break;

                case Response.Type.SYNC_EVENTS:
                    SyncEventsResponse syncEvents = Serializer.Deserialize<SyncEventsResponse>(stream);
                    response = new Response()
                    {
                        requestId = syncEvents.requestId,
                        commandID = syncEvents.commandID,
                        responseType = Response.Type.SYNC_EVENTS,
                        syncEventsResponse = syncEvents,
                    };
                    break;
              
                case Response.Type.SEARCH:
                    SearchResponse searchResponse = Serializer.Deserialize<SearchResponse>(stream);
                    response = new Response()
                    {
                        requestId = searchResponse.requestId,
                        commandID = searchResponse.commandID,
                        numberOfChuncks = searchResponse.numberOfChuncks,
                        sequenceId = searchResponse.sequenceId,
                        responseType = Response.Type.SEARCH,
                        search = searchResponse
                    };
                    break;
               
                case Response.Type.GET_SERIALIZATION_FORMAT:
                    GetSerializationFormatResponse getSerializationFormatResponse = Serializer.Deserialize<GetSerializationFormatResponse>(stream);
                    response = new Response()
                    {
                        requestId = getSerializationFormatResponse.requestId,
                        commandID = getSerializationFormatResponse.commandID,
                        responseType = Response.Type.GET_SERIALIZATION_FORMAT,
                        getSerializationFormatResponse = getSerializationFormatResponse
                    };
                    break;
                case Response.Type.INQUIRY_REQUEST_RESPONSE:
                    InquiryRequestResponse inquiryRequestResponse = Serializer.Deserialize<InquiryRequestResponse>(stream);
                    response = new Response()
                    {
                        requestId = inquiryRequestResponse.requestId,
                        commandID = inquiryRequestResponse.commandID,
                        responseType = Response.Type.INQUIRY_REQUEST_RESPONSE,
                        inquiryRequestResponse = inquiryRequestResponse
                    };
                    break;
                case Response.Type.GET_CONNECTED_CLIENTS:
                    GetConnectedClientsResponse getConnectedClientsResponse = Serializer.Deserialize<GetConnectedClientsResponse>(stream);
                    response = new Response()
                    {
                        requestId = getConnectedClientsResponse.requestId,
                        commandID = getConnectedClientsResponse.commandID,
                        responseType = Response.Type.GET_CONNECTED_CLIENTS,
                        getConnectedClientsResponse = getConnectedClientsResponse
                    };
                    break;
               
         
                default:
                    response = Serializer.Deserialize<Response>(stream);
                    break;
            }

            return response;
        }

        public static bool WriteRequestIdInResponse = true;

        public static IList SerializeInsertResponse(InsertResponse response, Response.Type responseType)
        {
            using (MemoryStream stream = new MemoryStream(50))
            {
                //TODO
                int requestIdLength = 0;
                if (WriteRequestIdInResponse)
                {
                    long requestId = response.requestId;
                    
                    byte[] requestBytes = BitConverter.GetBytes(requestId);
                    stream.Write(requestBytes, 0, 8);
                    requestIdLength += 8;
                }

                short value = (short)responseType;
                byte[] responseTypeBytes = BitConverter.GetBytes(value);
                stream.Write(responseTypeBytes, 0, responseTypeBytes.Length);

                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize(stream, response);

                int messageLen = (int)stream.Length - (size.Length + responseTypeBytes.Length + requestIdLength);
                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = requestIdLength;

                stream.Position += responseTypeBytes.Length;
                stream.Write(size, 0, size.Length);

                IList byteList = new ArrayList(1) { stream.ToArray() };

                return byteList;
            }
        }

        public static IList SerializeResponse(object response, Response.Type responseType)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                //TODO
                int requestIdLength = 0;
                if (WriteRequestIdInResponse)
                {
                    long requestId = 0;
                    switch (responseType)
                    {
                        case Response.Type.INSERT:
                            requestId = ((InsertResponse)response).requestId;
                            break;
                        case Response.Type.INSERT_BULK:
                            requestId = ((BulkInsertResponse)response).requestId;
                            break;
                        case Response.Type.GET_BULK:
                            requestId = ((BulkGetResponse)response).requestId;
                            break;
                        case Response.Type.EXECUTE_READER:
                            requestId = ((ExecuteReaderResponse)response).requestId;
                            break;
                        case Response.Type.GET_NEXT_CHUNK:
                            requestId = ((GetNextChunkResponse)response).requestId;
                            break;
                        case Response.Type.GET:
                            requestId = ((GetResponse)response).requestId;
                            break;
                        case Response.Type.DELETE:
                            requestId = ((DeleteResponse)response).requestId;
                            break;
                        case Response.Type.REMOVE:
                            requestId = ((RemoveResponse)response).requestId;
                            break;
                        case Response.Type.COUNT:
                            requestId = ((CountResponse)response).requestId;
                            break;
                        case Response.Type.ADD:
                            requestId = ((AddResponse)response).requestId;
                            break;
                        case Response.Type.ADD_BULK:
                            requestId = ((BulkAddResponse)response).requestId;
                            break;
                        case Response.Type.CLEAR:
                            requestId = ((ClearResponse)response).requestId;
                            break;
                        case Response.Type.CONTAINS_BULK:
                            requestId = ((ContainBulkResponse)response).requestId;
                            break;
                        case Response.Type.CONTAINS:
                            requestId = ((ContainResponse)response).requestId;
                            break;
                        case Response.Type.ADD_ATTRIBUTE:
                            requestId = ((AddAttributeResponse)response).requestId;
                            break;

                        case Response.Type.GET_CACHE_ITEM:
                            requestId = ((GetCacheItemResponse)response).requestId;
                            break;
                        case Response.Type.BULK_GET_CACHEITEM:
                            requestId = ((BulkGetCacheItemResponse)response).requestId;
                            break;
                        case Response.Type.REMOVE_GROUP:
                            requestId = ((RemoveGroupResponse)response).requestId;
                            break;

                        case Response.Type.GET_GROUP_NEXT_CHUNK:
                            requestId = ((GetGroupNextChunkResponse)response).requestId;
                            break;
                        case Response.Type.REMOVE_BULK:
                            requestId = ((BulkRemoveResponse)response).requestId;
                            break;
                        case Response.Type.DELETE_BULK:
                            requestId = ((BulkDeleteResponse)response).requestId;
                            break;
                        case Response.Type.REMOVE_TOPIC:
                            requestId = ((RemoveTopicResponse)response).requestId;
                            break;
                        case Response.Type.GET_TOPIC:
                            requestId = ((GetTopicResponse)response).requestId;
                            break;
                        case Response.Type.GET_MESSAGE:
                            requestId = ((GetMessageResponse)response).requestId;
                            break;
                        case Response.Type.MESSAGE_PUBLISH:
                            requestId = ((MessagePublishResponse)response).requestId;
                            break;
                        case Response.Type.MESSAGE_COUNT:
                            requestId = ((MessageCountResponse)response).requestId;
                            break;
                        case Response.Type.DISPOSE:
                            requestId = ((DisposeResponse)response).requestId;
                            break;
                        case Response.Type.DISPOSE_READER:
                            requestId = ((DisposeReaderResponse)response).requestId;
                            break;
                     
                      
                        case Response.Type.LOCK:
                            requestId = ((LockResponse)response).requestId;
                            break;
                        case Response.Type.UNLOCK:
                            requestId = ((UnlockResponse)response).requestId;
                            break;
                        case Response.Type.LOCK_VERIFY:
                            requestId = ((VerifyLockResponse)response).requestId;
                            break;
                        case Response.Type.ISLOCKED:
                            requestId = ((IsLockedResponse)response).requestId;
                            break;
                        case Response.Type.UNREGISTER_BULK_KEY_NOTIF:
                            requestId = ((UnregisterBulkKeyNotifResponse)response).requestId;
                            break;
                        case Response.Type.UNREGISTER_KEY_NOTIF:
                            requestId = ((UnregisterKeyNotifResponse)response).requestId;
                            break;
                        case Response.Type.REGISTER_BULK_KEY_NOTIF:
                            requestId = ((RegisterBulkKeyNotifResponse)response).requestId;
                            break;
                        case Response.Type.REGISTER_KEY_NOTIF:
                            requestId = ((RegisterKeyNotifResponse)response).requestId;
                            break;
                        case Response.Type.UNSUBSCRIBE_TOPIC:
                            requestId = ((UnSubscribeTopicResponse)response).requestId;
                            break;
                        case Response.Type.SUBSCRIBE_TOPIC:
                            requestId = ((SubscribeTopicResponse)response).requestId;
                            break;

                        case Response.Type.GET_READER_CHUNK:
                            requestId = ((GetReaderChunkResponse)response).requestId;
                            break;
                        case Response.Type.MESSAGE_ACKNOWLEDGEMENT:
                            requestId = ((MessageAcknowledgmentResponse)response).requestId;
                            break;
                        case Response.Type.RAISE_CUSTOM_EVENT:
                            requestId = ((RaiseCustomEventResponse)response).requestId;
                            break;
                        case Response.Type.GET_PRODUCT_VERSION:
                            requestId = ((GetProductVersionResponse)response).requestId;
                            break;
                        case Response.Type.PING:
                            requestId = ((PingResponse)response).requestId;
                            break;
                        case Response.Type.POLL:
                            requestId = ((PollResponse)response).requestId;
                            break;
                        case Response.Type.TOUCH:
                            requestId = ((TouchResponse)response).requestId;
                            break;
                        case Response.Type.REGISTER_POLL_NOTIF:
                            requestId = ((RegisterPollNotifResponse)response).requestId;
                            break;
                        case Response.Type.SYNC_EVENTS:
                            requestId = ((SyncEventsResponse)response).requestId;
                            break;
                        case Response.Type.SEARCH:
                            requestId = ((SearchResponse)response).requestId;
                            break;
                        case Response.Type.GET_SERIALIZATION_FORMAT:
                            requestId = ((GetSerializationFormatResponse)response).requestId;
                            break;
                        case Response.Type.INQUIRY_REQUEST_RESPONSE:
                            requestId = ((InquiryRequestResponse)response).requestId;
                            break;
                        case Response.Type.GET_CONNECTED_CLIENTS:
                            requestId = ((GetConnectedClientsResponse)response).requestId;
                            break;
                   
                    
                       
                        default:
                            requestId = ((Response)response).requestId;
                            break;
                    }
                    byte[] requestBytes = BitConverter.GetBytes(requestId);
                    stream.Write(requestBytes, 0, 8);
                    requestIdLength += 8;
                }

                short value = (short)responseType;
                byte[] responseTypeBytes = BitConverter.GetBytes(value);
                stream.Write(responseTypeBytes, 0, responseTypeBytes.Length);

                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                ProtoBuf.Serializer.Serialize(stream, response);

                int messageLen = (int)stream.Length - (size.Length + responseTypeBytes.Length + requestIdLength);
                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = requestIdLength;

                stream.Position += responseTypeBytes.Length;
                stream.Write(size, 0, size.Length);

                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public static IList SerializeResponse(Alachisoft.NCache.Common.Protobuf.Response command)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                //TODO
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);

                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.Response>(stream, command);

                int messageLen = (int)stream.Length - size.Length;
                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);

                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public static ClusteredArrayList SerializeResponse(Alachisoft.NCache.Common.Protobuf.ManagementResponse command)
        {
            using (ClusteredMemoryStream stream = new ClusteredMemoryStream())
            {
                byte[] size = new byte[10];
                stream.Write(size, 0, size.Length);
                
                Serializer.Serialize<Alachisoft.NCache.Common.Protobuf.ManagementResponse>(stream, command);
                int messageLen = (int)stream.Length - size.Length;

                size = UTF8Encoding.UTF8.GetBytes(messageLen.ToString());
                stream.Position = 0;
                stream.Write(size, 0, size.Length);
                ClusteredArrayList byteList = stream.GetInternalBuffer();

                return byteList;
            }
        }

        public static IList SerializeExceptionResponseWithType(Exception exc, long requestId, int commandID, int clientVersion)
        {
            Response response = GetExceptionResponse(exc, requestId, commandID);

            if (clientVersion >= 5000)
            {
                return SerializeResponse(response, Response.Type.EXCEPTION);
            }
            else
            {
                return SerializeResponse(response);
            }
        }

        public static byte[] SerializeSurrogateExceptionResponse(Exception exc, long requestId, int commandID, int clientVersion)
        {
            byte[] serializedResponse = null;
            Response response = GetExceptionResponse(exc, requestId, commandID);
            using (MemoryStream stream = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize(stream, response);
                serializedResponse = stream.ToArray();
            }
            return serializedResponse;
        }


        public static IList SerializeExceptionResponseWithoutType(Exception exc, long requestId, int commandID)
        {
            Response response = GetExceptionResponse(exc, requestId, commandID);

            return SerializeResponse(response);
        }

        private static Response GetExceptionResponse(Exception exc, long requestId, int commandID)
        {
            Alachisoft.NCache.Common.Protobuf.Exception ex = new Alachisoft.NCache.Common.Protobuf.Exception();
            ex.message = exc.Message;
            ex.exception = exc.ToString();
            if (exc is InvalidReaderException)
            {
                InvalidReaderException temp = (InvalidReaderException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.INVALID_READER_EXCEPTION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is OperationFailedException)
            {
                OperationFailedException temp = (OperationFailedException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.OPERATIONFAILED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is Runtime.Exceptions.AggregateException)
            {
                Runtime.Exceptions.AggregateException temp = (Runtime.Exceptions.AggregateException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.AGGREGATE;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is ConfigurationException)
            {
                ConfigurationException temp = (ConfigurationException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is SecurityException)
            {
                SecurityException temp = (SecurityException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.SECURITY;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }

            else if (exc is MaintenanceException)
            {
                MaintenanceException temp = (MaintenanceException)exc;
                ex.type = Protobuf.Exception.Type.MAINTENANCE_EXCEPTION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is VersionException)
            {
                VersionException tempEx = (VersionException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.CONFIGURATON_EXCEPTION;
                ex.errorCode = tempEx.ErrorCode;
                ex.stackTrace = tempEx.StackTrace;
            }
            else if (exc is OperationNotSupportedException)
            {
                OperationNotSupportedException temp = (OperationNotSupportedException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.NOTSUPPORTED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamAlreadyLockedException)
            {
                StreamAlreadyLockedException temp = (StreamAlreadyLockedException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_ALREADY_LOCKED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamCloseException)
            {
                StreamCloseException temp = (StreamCloseException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_CLOSED;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamInvalidLockException)
            {
                StreamInvalidLockException temp = (StreamInvalidLockException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_INVALID_LOCK;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamNotFoundException)
            {
                StreamNotFoundException temp = (StreamNotFoundException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_NOT_FOUND;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is StreamException)
            {
                StreamException temp = (StreamException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STREAM_EXC;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if (exc is TypeIndexNotDefined)
            {
                TypeIndexNotDefined temp = (TypeIndexNotDefined)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.TYPE_INDEX_NOT_FOUND;
         //to do 
            }
            else if (exc is AttributeIndexNotDefined)
            {
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.ATTRIBUTE_INDEX_NOT_FOUND;
            }
            else if (exc is StateTransferInProgressException)
            {
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.STATE_TRANSFER_EXCEPTION;
            }
            else if (exc is LicensingException)
            {
                LicensingException temp = (LicensingException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.LICENSING_EXCEPTION;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else if(exc is CacheException)
            {
                CacheException temp = (CacheException)exc;
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;
                ex.errorCode = temp.ErrorCode;
                ex.stackTrace = temp.StackTrace;
            }
            else
            {
                ex.type = Alachisoft.NCache.Common.Protobuf.Exception.Type.GENERALFAILURE;
                ex.stackTrace = exc.StackTrace; 
            }

            Alachisoft.NCache.Common.Protobuf.Response response = new Alachisoft.NCache.Common.Protobuf.Response();
            response.requestId = requestId;

            response.commandID = commandID;
            response.exception = ex;
            response.responseType = Alachisoft.NCache.Common.Protobuf.Response.Type.EXCEPTION;

            return response;
        }

        public static void SetResponse(ResponseBase response, long requestId, int commandId, Alachisoft.NCache.Common.Protobuf.Response.Type? responseType = null)
        {
            response.requestId = requestId;
            response.commandID = commandId;

            if (response is Response)
                ((Response)response).responseType = responseType.Value;
        }
    }
}
