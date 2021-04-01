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

using Alachisoft.NCache.Common.Pooling.Lease;
using Alachisoft.NCache.Common.Pooling.Extension;

namespace Alachisoft.NCache.Common.Protobuf
{
    public partial class Response : SimpleLease
    {
        #region ILeasable

        public override sealed void ResetLeasable()
        {
            switch (responseType)
            {
                case Type.LEGACY_RESPONSE:
                    break;

                case Type.ADD:
                    addResponse = null;
                    break;
                    
                case Type.ADD_BULK:
                    bulkAdd = null;
                    break;

                case Type.GET_BULK:
                    bulkGet = null;
                    break;

                case Type.INSERT_BULK:
                    bulkInsert = null;
                    break;

                case Type.REMOVE_BULK:
                    bulkRemove = null;
                    break;

                case Type.CLEAR:
                    clearResponse = null;
                    break;

                case Type.CONTAINS:
                    contain = null;
                    break;

                case Type.COUNT:
                    count = null;
                    break;

                case Type.DISPOSE:
                    disposeResponse = null;
                    break;

                case Type.GET_CACHE_ITEM:
                    getItem = null;
                    break;

                case Type.GET:
                    get = null;
                    break;

                case Type.GET_COMPACT_TYPES:
                    getCompactTypes = null;
                    break;

                case Type.GET_ENUMERATOR:
                    getEnum = null;
                    break;

               
                case Type.GET_HASHMAP:
                    getHashmap = null;
                    break;

                case Type.GET_OPTIMAL_SERVER:
                    getOptimalServer = null;
                    break;

                case Type.GET_TYPEINFO_MAP:
                    getTypemap = null;
                    break;

                case Type.INIT:
                    initCache = null;
                    break;

                case Type.INSERT:
                    insert = null;
                    break;

                case Type.RAISE_CUSTOM_EVENT:
                    raiseCustomEventResponse = null;
                    break;

                case Type.REGISTER_KEY_NOTIF:
                    registerKeyNotifResponse = null;
                    break;

                case Type.REGISTER_NOTIF:
                    registerNotifResponse = null;
                    break;

                case Type.REMOVE:
                    remove = null;
                    break;

                case Type.SEARCH:
                    search = null;
                    break;

                case Type.LOCK:
                    lockResponse = null;
                    break;

                case Type.UNLOCK:
                    unlockResponse = null;
                    break;

                case Type.ISLOCKED:
                    isLockedResponse = null;
                    break;

                case Type.LOCK_VERIFY:
                    lockVerify = null;
                    break;

                case Type.UNREGISTER_KEY_NOTIF:
                    unregisterKeyNotifResponse = null;
                    break;

                case Type.UNREGISTER_BULK_KEY_NOTIF:
                    unregisterBulkKeyNotifResponse = null;
                    break;

                case Type.REGISTER_BULK_KEY_NOTIF:
                    registerBulkKeyNotifResponse = null;
                    break;

                case Type.GET_LOGGING_INFO:
                    getLoggingInfoResponse = null;
                    break;

                case Type.EXCEPTION:
                    exception = null;
                    break;

                case Type.ASYNC_OP_COMPLETED_CALLBACK:
                    asyncOpCompletedCallback = null;
                    break;

                case Type.ITEM_REMOVED_CALLBACK:
                    itemRemovedCallback = null;
                    break;

                case Type.ITEM_UPDATED_CALLBACK:
                    itemUpdatedCallback = null;
                    break;
                    
                case Type.CACHE_CLEARED_EVENT:
                    cacheCleared = null;
                    break;

                case Type.CACHE_STOPPED_EVENT:
                    cacheStopped = null;
                    break;

                case Type.CONFIG_MODIFIED_EVENT:
                    configModified = null;
                    break;

                case Type.CUSTOM_EVENT:
                    customEvent = null;
                    break;

                case Type.HASHMAP_CHANGED_EVENT:
                    hashmapChanged = null;
                    break;

                case Type.ITEM_ADDED_EVENT:
                    itemAdded = null;
                    break;

                case Type.ITEM_REMOVED_EVENT:
                    itemRemoved = null;
                    break;

                case Type.ITEM_UPDATED_EVENT:
                    itemUpdated = null;
                    break;

                case Type.LOGGING_INFO_MODIFIED_EVENT:
                    loggingInfoModified = null;
                    break;

                case Type.NODE_JOINED_EVENT:
                    nodeJoined = null;
                    break;

                case Type.NODE_LEFT_EVENT:
                    nodeLeft = null;
                    break;

                case Type.QUEUE_FULL_EVENT:
                    queueFullEvent = null;
                    break;

                case Type.DS_UPDATE_CALLBACK:
                    dsUpdateCallbackRespose = null;
                    break;

                case Type.HEART_BEAT:
                    heartBeatResponse = null;
                    break;

                case Type.DELETE_BULK:
                    bulkDeleteResponse = null;
                    break;

                case Type.DELETE:
                    deleteResponse = null;
                    break;

                case Type.GET_NEXT_CHUNK:
                    getNextChunkResponse = null;
                    break;

                case Type.GET_GROUP_NEXT_CHUNK:
                    getGroupNextChunkResponse = null;
                    break;

              

                case Type.ADD_ATTRIBUTE:
                    addAttributeResponse = null;
                    break;

                case Type.COMPACT_TYPE_REGISTER_EVENT:
                    compactTypeRegisterEvent = null;
                    break;

                case Type.GET_RUNNING_SERVERS:
                    getRunningServer = null;
                    break;

                case Type.BULK_EVENT:
                    bulkEventResponse = null;
                    break;

                case Type.SYNC_EVENTS:
                    syncEventsResponse = null;
                    break;

                case Type.BLOCK_ACTIVITY:
                    blockActivityEvent = null;
                    break;

                case Type.UNBLOCK_ACTIVITY:
                    unblockActivityEvent = null;
                    break;

        

                case Type.GET_PRODUCT_VERSION:
                    getProductVersionResponse = null;
                    break;
                    
                case Type.RESPONSE_FRAGMENT:
                    getResponseFragment = null;
                    break;

                case Type.INQUIRY_REQUEST_RESPONSE:
                    inquiryRequestResponse = null;
                    break;

                case Type.GET_SERVER_MAPPING:
                    getServerMappingResponse = null;
                    break;

                case Type.GET_CACHE_BINDING:
                    getCacheBindingResponse = null;
                    break;
                    
                case Type.DISPOSE_READER:
                    disposeReaderResponse = null;
                    break;

                case Type.EXECUTE_READER:
                    executeReaderResponse = null;
                    break;

                case Type.GET_READER_CHUNK:
                    getReaderChunkResponse = null;
                    break;

               
                case Type.LC_DATA:
                    LcDataResponse = null;
                    break;

                case Type.POLL:
                    pollResponse = null;
                    break;

                case Type.REGISTER_POLL_NOTIF:
                    registerPollNotifResponse = null;
                    break;

                case Type.POLL_NOTIFY_CALLBACK:
                    pollNotifyEventResponse = null;
                    break;

                case Type.GET_CONNECTED_CLIENTS:
                    getConnectedClientsResponse = null;
                    break;

                case Type.CLIENT_CONNECTIVITY_EVENT:
                    break;

                case Type.TOUCH:
                    touchResponse = null;
                    break;

                case Type.GET_CACHE_MANAGEMENT_PORT:
                    getCacheManagementPortResponse = null;
                    break;

                case Type.GET_TOPIC:
                    getTopicResponse = null;
                    break;

                case Type.SUBSCRIBE_TOPIC:
                    subscribeTopicResponse = null;
                    break;

                case Type.REMOVE_TOPIC:
                    removeTopicResponse = null;
                    break;

                case Type.UNSUBSCRIBE_TOPIC:
                    unSubscribeTopicResponse = null;
                    break;

                case Type.MESSAGE_PUBLISH:
                    messagePublishResponse = null;
                    break;

                case Type.GET_MESSAGE:
                    getMessageResponse = null;
                    break;

                case Type.MESSAGE_ACKNOWLEDGEMENT:
                    messageAcknowledgmentResponse = null;
                    break;

                case Type.PING:
                    pingResponse = null;
                    break;

                case Type.OPERATIONCHANGEDEVNET:
                    operationModeChangeEventResponse = null;
                    break;

                case Type.MESSAGE_COUNT:
                    messageCountResponse = null;
                    break;

                case Type.GET_SERIALIZATION_FORMAT:
                    getSerializationFormatResponse = null;
                    break;

                case Type.BULK_GET_CACHEITEM:
                    bulkGetCacheItem = null;
                    break;

                case Type.CONTAINS_BULK:
                    containBulkResponse = null;
                    break;

                default:
                    throw new System.Exception($"Case not handled for response type '{responseType}' in order to reset it.");
            }

            sequenceId = 1;
            numberOfChuncks = 1;
            responseType = Type.ADD;
            commandID = default(int);
            requestId = default(long);
            intendedRecipient = string.Empty;
            extensionObject = default(ProtoBuf.IExtension);
        }

        public override sealed void ReturnLeasableToPool()
        {
            switch (responseType)
            {
                case Type.LEGACY_RESPONSE:
                    break;

                case Type.ADD:
                    (addResponse as SimpleLease)?.ReturnLeasableToPool();
                    break;
                    
                case Type.ADD_BULK:
                    break;

                case Type.GET_BULK:
                    break;

                case Type.INSERT_BULK:
                    break;

                case Type.REMOVE_BULK:
                    break;

                case Type.CLEAR:
                    break;

                case Type.CONTAINS:
                    break;

                case Type.COUNT:
                    break;

                case Type.DISPOSE:
                    break;

                case Type.GET_CACHE_ITEM:
                    break;

                case Type.GET:
                    (get as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_COMPACT_TYPES:
                    break;

                case Type.GET_ENUMERATOR:
                    break;

                case Type.GET_HASHMAP:
                    break;

                case Type.GET_OPTIMAL_SERVER:
                    break;

                case Type.GET_TYPEINFO_MAP:
                    break;

                case Type.INIT:
                    break;

                case Type.INSERT:
                    (insert as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.RAISE_CUSTOM_EVENT:
                    break;

                case Type.REGISTER_KEY_NOTIF:
                    break;

                case Type.REGISTER_NOTIF:
                    break;

                case Type.REMOVE:
                    (remove as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.SEARCH:
                    break;

                case Type.LOCK:
                    break;

                case Type.UNLOCK:
                    break;

                case Type.ISLOCKED:
                    break;

                case Type.LOCK_VERIFY:
                    break;

                case Type.UNREGISTER_KEY_NOTIF:
                    break;

                case Type.UNREGISTER_BULK_KEY_NOTIF:
                    break;

                case Type.REGISTER_BULK_KEY_NOTIF:
                    break;

                case Type.GET_LOGGING_INFO:
                    break;

                case Type.SEARCH_ENTRIES:
                    break;

                case Type.EXCEPTION:
                    break;

                case Type.ASYNC_OP_COMPLETED_CALLBACK:
                    break;

                case Type.ITEM_REMOVED_CALLBACK:
                    break;

                case Type.ITEM_UPDATED_CALLBACK:
                    break;

                case Type.CACHE_CLEARED_EVENT:
                    break;

                case Type.CACHE_STOPPED_EVENT:
                    break;

                case Type.CONFIG_MODIFIED_EVENT:
                    break;

                case Type.CUSTOM_EVENT:
                    break;

                case Type.HASHMAP_CHANGED_EVENT:
                    break;

                case Type.ITEM_ADDED_EVENT:
                    break;

                case Type.ITEM_REMOVED_EVENT:
                    break;

                case Type.ITEM_UPDATED_EVENT:
                    break;

                case Type.LOGGING_INFO_MODIFIED_EVENT:
                    break;

                case Type.NODE_JOINED_EVENT:
                    break;

                case Type.NODE_LEFT_EVENT:
                    break;

                case Type.QUEUE_FULL_EVENT:
                    break;

                case Type.DS_UPDATE_CALLBACK:
                    break;

                case Type.HEART_BEAT:
                    break;
                case Type.CQ_CALLBACK:
                    break;

                case Type.UNREGISTER_CQ:
                    break;

                case Type.REGISTER_CQ:
                    break;

                case Type.DELETE_BULK:
                    break;

                case Type.DELETE:
                    break;

                case Type.GET_NEXT_CHUNK:
                    break;

                case Type.GET_GROUP_NEXT_CHUNK:
                    break;

                case Type.SEARCH_CQ:
                    break;

                case Type.SEARCH_ENTRIES_CQ:
                    break;

                case Type.ADD_ATTRIBUTE:
                    break;

                case Type.COMPACT_TYPE_REGISTER_EVENT:
                    break;

                case Type.GET_RUNNING_SERVERS:
                    break;

                case Type.BULK_EVENT:
                    break;

                case Type.SYNC_EVENTS:
                    break;

                case Type.BLOCK_ACTIVITY:
                    break;

                case Type.UNBLOCK_ACTIVITY:
                    break;

                case Type.REMOVE_QUERY:
                    break;

                case Type.DELETE_QUERY:
                    break;

                case Type.GET_PRODUCT_VERSION:
                    break;
                    
                case Type.RESPONSE_FRAGMENT:
                    break;

                case Type.INQUIRY_REQUEST_RESPONSE:
                    break;

                case Type.GET_SERVER_MAPPING:
                    break;

                case Type.GET_CACHE_BINDING:
                    break;
                    
                case Type.DISPOSE_READER:
                    break;

                case Type.EXECUTE_READER:
                    break;

                case Type.GET_READER_CHUNK:
                    break;

                case Type.EXECUTE_READER_CQ:
                    break;

                case Type.EXPIRATION_RESPONSE:
                    break;

                case Type.LC_DATA:
                    break;

                case Type.POLL:
                    break;

                case Type.REGISTER_POLL_NOTIF:
                    break;

                case Type.POLL_NOTIFY_CALLBACK:
                    break;

                case Type.GET_CONNECTED_CLIENTS:
                    break;

                case Type.CLIENT_CONNECTIVITY_EVENT:
                    break;

                case Type.TOUCH:
                    break;

                case Type.GET_CACHE_MANAGEMENT_PORT:
                    break;

                case Type.GET_TOPIC:
                    break;

                case Type.SUBSCRIBE_TOPIC:
                    break;

                case Type.REMOVE_TOPIC:
                    break;

                case Type.UNSUBSCRIBE_TOPIC:
                    break;

                case Type.MESSAGE_PUBLISH:
                    break;

                case Type.GET_MESSAGE:
                    break;

                case Type.MESSAGE_ACKNOWLEDGEMENT:
                    break;

                case Type.PING:
                    break;

                case Type.OPERATIONCHANGEDEVNET:
                    break;

                case Type.MESSAGE_COUNT:
                    break;

                case Type.GET_SERIALIZATION_FORMAT:
                    break;

                case Type.BULK_GET_CACHEITEM:
                    break;

                case Type.CONTAINS_BULK:
                    break;

                default:
                    throw new System.Exception($"Case not handled for response type '{responseType}' in order to return to pool.");
            }
            PoolManager.GetProtobufResponsePool()?.Return(this);
        }

        #endregion
    }
}
