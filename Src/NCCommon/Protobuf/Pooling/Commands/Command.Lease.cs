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
                    break;

                case Type.ADD_SYNC_DEPENDENCY:
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
                    (getCommand as SimpleLease)?.ReturnLeasableToPool();
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
                    (insertCommand as SimpleLease)?.ReturnLeasableToPool();
                    break;

                case Type.RAISE_CUSTOM_EVENT:
                    break;

                case Type.REGISTER_KEY_NOTIF:
                    break;

                case Type.REGISTER_NOTIF:
                    break;

                case Type.REMOVE:
                    (removeCommand as SimpleLease)?.ReturnLeasableToPool();
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


                case Type.UNREGISTER_CQ:
                    break;

                case Type.SEARCH_CQ:
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

                case Type.ADD_ATTRIBUTE:
                    break;

                case Type.GET_RUNNING_SERVERS:
                    break;

                case Type.SYNC_EVENTS:
                    break;

                case Type.DELETEQUERY:
                    break;

                case Type.GET_PRODUCT_VERSION:
                    break;
                    
                case Type.GET_SERVER_MAPPING:
                    break;

                case Type.INQUIRY_REQUEST:
                    break;

                case Type.GET_CACHE_BINDING:
                    break;
                    
                case Type.EXECUTE_READER:
                    break;

                case Type.GET_READER_CHUNK:
                    break;

                case Type.DISPOSE_READER:
                    break;

                case Type.EXECUTE_READER_CQ:
                    break;

                case Type.GET_EXPIRATION:
                    break;

                case Type.GET_LC_DATA:
                    break;

                case Type.POLL:
                    break;

                case Type.REGISTER_POLLING_NOTIFICATION:
                    break;

                case Type.GET_CONNECTED_CLIENTS:
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

                case Type.MESSAGE_ACKNOWLEDGMENT:
                    break;

                case Type.PING:
                    break;

                case Type.MESSAGE_COUNT:
                    break;

                case Type.GET_SERIALIZATION_FORMAT:
                    break;

                case Type.GET_BULK_CACHEITEM:
                    break;

                case Type.CONTAINS_BULK:
                    break;

                default:
                    throw new System.Exception($"Case not handled for command type '{type}' in order to return to pool.");
            }
            PoolManager.GetProtobufCommandPool()?.Return(this);
        }

        #endregion
    }
}