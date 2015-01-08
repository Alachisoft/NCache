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
namespace Alachisoft.NCache.Web.Command
{
    enum CommandType
    {
        ADD = 1,
        ADD_BULK = 2,
        GET_BULK = 3,
        INSERT_BULK = 4,
        REMOVE_BULK = 5,
        CLEAR = 6,
        CONTAINS = 7,
        COUNT = 8,
        DISPOSE = 9,
        GET_CACHE_ITEM = 10,
        GET = 11,
        GET_ENUMERATOR = 12,
        GET_HASHMAP = 13,
        GET_OPTIMAL_SERVER = 14,
        GET_THRESHOLD_SIZE = 15,
        GET_TYPEINFO_MAP = 16,
        INIT = 17,
        INSERT = 18,
        RAISE_CUSTOM_EVENT = 19,
        REGISTER_KEY_NOTIF = 20,
        REGISTER_NOTIF = 21,
        REMOVE = 22,
        SEARCH = 23,
        LOCK = 24,
        UNLOCK = 25,
        ISLOCKED = 26,
        LOCK_VERIFY = 27,
        UNREGISTER_KEY_NOTIF = 28,
        UNREGISTER_BULK_KEY_NOTIF = 29,
        REGISTER_BULK_KEY_NOTIF = 30,
        GET_LOGGING_INFO = 31,
        INIT_SECONDARY = 32,
        DELETE_BULK = 33,
        DELETE = 34,
        GET_NEXT_CHUNK = 35,
        ADD_ATTRIBUTE = 36,
        SYNC_EVENTS = 37,
        GET_PRODUCT_VERSION = 38,
        GET_SERVER_MAPPING = 39,
        GET_RUNNING_SERVERS = 40
    }
}
