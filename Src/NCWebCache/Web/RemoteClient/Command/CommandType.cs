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

namespace Alachisoft.NCache.Web.Command
{
    enum CommandType
    {
        ADD = 1,
        ADD_DEPENDENCY = 2,
        ADD_SYNC_DEPENDENCY = 3,
        ADD_BULK = 4,
        GET_BULK = 5,
        INSERT_BULK = 6,
        REMOVE_BULK = 7,
        CLEAR = 8,
        CONTAINS = 9,
        COUNT = 10,
        DISPOSE = 11,
        GET_CACHE_ITEM = 12,
        GET = 13,
        GET_COMPACT_TYPES = 14,
        GET_ENUMERATOR = 15,
        GET_GROUP = 16,
        GET_HASHMAP = 17,
        GET_OPTIMAL_SERVER = 18,
        GET_THRESHOLD_SIZE = 19,
        GET_TYPEINFO_MAP = 20,
        INIT = 21,
        INSERT = 22,
        RAISE_CUSTOM_EVENT = 23,
        REGISTER_KEY_NOTIF = 24,
        REGISTER_NOTIF = 25,
        REMOVE = 26,
        REMOVE_GROUP = 27,
        SEARCH = 28,
        GET_TAG = 29,
        LOCK = 30,
        UNLOCK = 31,
        ISLOCKED = 32,
        LOCK_VERIFY = 33,
        UNREGISTER_KEY_NOTIF = 34,
        UNREGISTER_BULK_KEY_NOTIF = 35,
        REGISTER_BULK_KEY_NOTIF = 36,
        HYBRID_BULK = 37,
        GET_LOGGING_INFO = 38,
        CLOSE_STREAM = 39,
        GET_STREAM_LENGTH = 40,
        OPEN_STREAM = 41,
        WRITE_TO_STREAM = 42,
        READ_FROM_STREAM = 43,
        REMOVE_BY_TAG = 52,
        UNREGISTER_CQ = 54,
        SEARCH_CQ = 55,
        REGISTER_CQ = 56,
        INIT_SECONDARY = 57,
        GET_KEYS_TAG = 58,
        DELETE_BULK = 59,
        DELETE = 60,
        GET_NEXT_CHUNK = 61,
        GETGROUP_NEXT_CHUNK = 62,
        ADD_ATTRIBUTE = 63,
        GET_ENCRYPTION = 64,
        GET_RUNNING_SERVERS = 65,
        SYNC_EVENTS = 66,
        DELETEQUERY = 67,
        GET_PRODUCT_VERSION = 68,
        GET_SERVER_MAPPING = 69,
        INQUIRY_REQUEST = 70,
        GET_CACHE_BINDING = 71,
        MAP_REDUCE_TASK = 72,
        CANCEL_TASK = 73,
        REGISTER_TASK_CALLBACK = 74,
        RUNNING_TASKS = 75,
        TASK_PROGRESS = 76,
        TASK_ENUMERATOR = 77,
        TASK_NEXT_RECORD = 78,
        INVOKE_ENTRY_PROCESSOR = 79,
        EXECUTE_READER = 80,
        GET_READER_CHUNK = 81,
        DISPOSE_READER = 82,
        EXECUTE_READER_CQ = 83,
        GET_EXPIRATION = 84,
        POLL = 85,
        REGISTER_POLLING_NOTIFICATION = 86,

        GET_CONNECTED_CLIENTS = 88,
        TOUCH = 89,
        GET_CACHE_MANAGEMENT_PORT = 90,
        GET_TOPIC = 91,
        SUBCRIBE = 92,
        REMOVE_TOPIC = 93,
        UNSUBCRIBE = 94,
        PUBLISHMESSAGE = 95,
        GETMESSAGE = 96,
        MESSAGE_ACKNOWLEDGMENT = 97,
        PING = 98
    }
}
