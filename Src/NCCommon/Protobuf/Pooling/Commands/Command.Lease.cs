//  Copyright (c) 2019 Alachisoft
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
    public partial class Command : SimpleLease
    {
        #region ILeasable

        public override sealed void ResetLeasable()
        {
            switch (type)
            {
                case Type.ADD:
                    addCommand = null;
                    break;

                case Type.ADD_BULK:
                    bulkAddCommand = null;
                    break;

                case Type.GET_BULK:
                    bulkGetCommand = null;
                    break;

                case Type.INSERT_BULK:
                    bulkInsertCommand = null;
                    break;

                case Type.REMOVE_BULK:
                    bulkRemoveCommand = null;
                    break;

                case Type.CLEAR:
                    clearCommand = null;
                    break;

                case Type.CONTAINS:
                    containsCommand = null;
                    break;

                case Type.COUNT:
                    countCommand = null;
                    break;

                case Type.DISPOSE:
                    disposeCommand = null;
                    break;

                case Type.GET_CACHE_ITEM:
                    getCacheItemCommand = null;
                    break;

                case Type.GET:
                    getCommand = null;
                    break;

                case Type.GET_COMPACT_TYPES:
                    getCompactTypesCommand = null;
                    break;

                case Type.GET_ENUMERATOR:
                    getEnumeratorCommand = null;
                    break;

               

                case Type.GET_HASHMAP:
                    getHashmapCommand = null;
                    break;

                case Type.GET_OPTIMAL_SERVER:
                    getOptimalServerCommand = null;
                    break;

                case Type.GET_TYPEINFO_MAP:
                    getTypeInfoMapCommand = null;
                    break;

                case Type.INIT:
                    initCommand = null;
                    break;

                case Type.INSERT:
                    insertCommand = null;
                    break;

                case Type.RAISE_CUSTOM_EVENT:
                    raiseCustomEventCommand = null;
                    break;

                case Type.REGISTER_KEY_NOTIF:
                    registerKeyNotifCommand = null;
                    break;

                case Type.REGISTER_NOTIF:
                    registerNotifCommand = null;
                    break;

                case Type.REMOVE:
                    removeCommand = null;
                    break;

                case Type.LOCK:
                    lockCommand = null;
                    break;

                case Type.UNLOCK:
                    unlockCommand = null;
                    break;

                case Type.ISLOCKED:
                    isLockedCommand = null;
                    break;

                case Type.LOCK_VERIFY:
                    lockVerifyCommand = null;
                    break;

                case Type.UNREGISTER_KEY_NOTIF:
                    unRegisterKeyNotifCommand = null;
                    break;

                case Type.UNREGISTER_BULK_KEY_NOTIF:
                    unRegisterBulkKeyNotifCommand = null;
                    break;

                case Type.REGISTER_BULK_KEY_NOTIF:
                    registerBulkKeyNotifCommand = null;
                    break;
                    
                case Type.GET_LOGGING_INFO:
                    getLoggingInfoCommand = null;
                    break;

                case Type.DELETE_BULK:
                    bulkDeleteCommand = null;
                    break;

                case Type.DELETE:
                    deleteCommand = null;
                    break;

                case Type.GET_NEXT_CHUNK:
                    getNextChunkCommand = null;
                    break;

                case Type.GET_GROUP_NEXT_CHUNK:
                    getGroupNextChunkCommand = null;
                    break;

                case Type.ADD_ATTRIBUTE:
                    addAttributeCommand = null;
                    break;

                case Type.GET_RUNNING_SERVERS:
                    getRunningServersCommand = null;
                    break;

                case Type.SYNC_EVENTS:
                    syncEventsCommand = null;
                    break;

                case Type.DELETEQUERY:
                    deleteQueryCommand = null;
                    break;

                case Type.GET_PRODUCT_VERSION:
                    getProductVersionCommand = null;
                    break;
                    
                case Type.GET_SERVER_MAPPING:
                    getServerMappingCommand = null;
                    break;

                case Type.INQUIRY_REQUEST:
                    inquiryRequestCommand = null;
                    break;

                case Type.GET_CACHE_BINDING:
                    getCacheBindingCommand = null;
                    break;
               
                case Type.GET_READER_CHUNK:
                    getReaderNextChunkCommand = null;
                    break;

                case Type.DISPOSE_READER:
                    disposeReaderCommand = null;
                    break;
                    

                case Type.GET_EXPIRATION:
                    getExpirationCommand = null;
                    break;

                case Type.GET_LC_DATA:
                    getLCCommand = null;
                    break;

                case Type.POLL:
                    pollCommand = null;
                    break;

                case Type.REGISTER_POLLING_NOTIFICATION:
                    registerPollNotifCommand = null;
                    break;

                case Type.GET_CONNECTED_CLIENTS:
                    getConnectedClientsCommand = null;
                    break;

                case Type.TOUCH:
                    touchCommand = null;
                    break;

                case Type.GET_CACHE_MANAGEMENT_PORT:
                    getCacheManagementPortCommand = null;
                    break;

                case Type.GET_TOPIC:
                    getTopicCommand = null;
                    break;

                case Type.SUBSCRIBE_TOPIC:
                    subscribeTopicCommand = null;
                    break;

                case Type.REMOVE_TOPIC:
                    removeTopicCommand = null;
                    break;

                case Type.UNSUBSCRIBE_TOPIC:
                    unSubscribeTopicCommand = null;
                    break;

                case Type.MESSAGE_PUBLISH:
                    messagePublishCommand = null;
                    break;

                case Type.GET_MESSAGE:
                    getMessageCommand = null;
                    break;

                case Type.MESSAGE_ACKNOWLEDGMENT:
                    mesasgeAcknowledgmentCommand = null;
                    break;

                case Type.PING:
                    pingCommand = null;
                    break;

                case Type.MESSAGE_COUNT:
                    messageCountCommand = null;
                    break;

                case Type.GET_SERIALIZATION_FORMAT:
                    getSerializationFormatCommand = null;
                    break;

                case Type.GET_BULK_CACHEITEM:
                    bulkGetCacheItemCommand = null;
                    break;

                case Type.CONTAINS_BULK:
                    containsBulkCommand = null;
                    break;

                default:
                    throw new System.Exception($"Case not handled for command type '{type}' in order to reset it.");
            }

            commandID = -1;
            type = Type.ADD;
            commandVersion = 0;
            MethodOverload = 0;
            clientLastViewId = -1;
            version = string.Empty;
            isRetryCommand = false;
            requestID = default(long);
            intendedRecipient = string.Empty;
            extensionObject = default(ProtoBuf.IExtension);
        }

        public override sealed void ReturnLeasableToPool()
        {
            switch (type)
            {
                case Type.ADD:
                    (addCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.ADD_DEPENDENCY:
                    // (addDependencyCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.ADD_SYNC_DEPENDENCY:
                    // (addSyncDependencyCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.ADD_BULK:
                    // (bulkAddCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_BULK:
                    // (bulkGetCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.INSERT_BULK:
                    // (bulkInsertCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REMOVE_BULK:
                    // (bulkRemoveCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.CLEAR:
                    // (clearCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.CONTAINS:
                    // (containsCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.COUNT:
                    // (countCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.DISPOSE:
                    // (disposeCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_CACHE_ITEM:
                    // (getCacheItemCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET:
                    (getCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_COMPACT_TYPES:
                    // (getCompactTypesCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_ENUMERATOR:
                    // (getEnumeratorCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_HASHMAP:
                    // (getHashmapCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_OPTIMAL_SERVER:
                    // (getOptimalServerCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_TYPEINFO_MAP:
                    // (getTypeInfoMapCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.INIT:
                    // (initCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.INSERT:
                    (insertCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.RAISE_CUSTOM_EVENT:
                    // (raiseCustomEventCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REGISTER_KEY_NOTIF:
                    // (registerKeyNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REGISTER_NOTIF:
                    // (registerNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REMOVE:
                    (removeCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;


                case Type.SEARCH:
                    // (searchCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.LOCK:
                    // (lockCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.UNLOCK:
                    // (unlockCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.ISLOCKED:
                    // (isLockedCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.LOCK_VERIFY:
                    // (lockVerifyCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.UNREGISTER_KEY_NOTIF:
                    // (unRegisterKeyNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.UNREGISTER_BULK_KEY_NOTIF:
                    // (unRegisterBulkKeyNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REGISTER_BULK_KEY_NOTIF:
                    // (registerBulkKeyNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;
                    
                case Type.GET_LOGGING_INFO:
                    // (getLoggingInfoCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;


                case Type.UNREGISTER_CQ:
                    // (unRegisterCQCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.SEARCH_CQ:
                    // (searchCQCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REGISTER_CQ:
                    // (registerCQCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.DELETE_BULK:
                    // (bulkDeleteCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.DELETE:
                    // (deleteCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_NEXT_CHUNK:
                    // (getNextChunkCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_GROUP_NEXT_CHUNK:
                    // (getGroupNextChunkCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.ADD_ATTRIBUTE:
                    // (addAttributeCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_RUNNING_SERVERS:
                    // (getRunningServersCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.SYNC_EVENTS:
                    // (syncEventsCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.DELETEQUERY:
                    // (deleteQueryCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_PRODUCT_VERSION:
                    // (getProductVersionCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;
                    
                case Type.GET_SERVER_MAPPING:
                    // (getServerMappingCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.INQUIRY_REQUEST:
                    // (inquiryRequestCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_CACHE_BINDING:
                    // (getCacheBindingCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;
                    
                case Type.EXECUTE_READER:
                    // (executeReaderCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_READER_CHUNK:
                    // (getReaderNextChunkCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.DISPOSE_READER:
                    // (disposeReaderCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.EXECUTE_READER_CQ:
                    // (executeReaderCQCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_EXPIRATION:
                    // (getExpirationCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_LC_DATA:
                    // (getLCCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.POLL:
                    // (pollCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REGISTER_POLLING_NOTIFICATION:
                    // (registerPollNotifCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_CONNECTED_CLIENTS:
                    // (getConnectedClientsCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.TOUCH:
                    // (touchCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_CACHE_MANAGEMENT_PORT:
                    // (getCacheManagementPortCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_TOPIC:
                    // (getTopicCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.SUBSCRIBE_TOPIC:
                    // (subscribeTopicCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.REMOVE_TOPIC:
                    // (removeTopicCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.UNSUBSCRIBE_TOPIC:
                    // (unSubscribeTopicCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.MESSAGE_PUBLISH:
                    // (messagePublishCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_MESSAGE:
                    // (getMessageCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.MESSAGE_ACKNOWLEDGMENT:
                    // (mesasgeAcknowledgmentCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.PING:
                    // (pingCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.MESSAGE_COUNT:
                    // (messageCountCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_SERIALIZATION_FORMAT:
                    // (getSerializationFormatCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.GET_BULK_CACHEITEM:
                    // (bulkGetCacheItemCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.CONTAINS_BULK:
                    // (containsBulkCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                default:
                    throw new System.Exception($"Case not handled for command type '{type}' in order to return to pool.");
            }
            PoolManager.GetProtobufCommandPool()?.Return(this);
        }

        #endregion
    }
}