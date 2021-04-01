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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Common.Monitoring
{
    public class MethodsName
    {
        public const string ADD = "add";
        public const string ADDASYNC = "addasync";
        public const string ADDBULK = "addbulk";
        public const string INSERT = "insert";
        public const string INSERTASYNC = "insertasync";
        public const string INSERTBULK = "insertbulk";
        public const string REMOVE = "remove";
        public const string REMOVEBULK = "removebulk";
        public const string DELETE = "delete";
        public const string DELETEASYNC = "deleteasync";
        public const string DELETEBULK = "deletebulk";
        public const string AddDEPENDENCY = "AddDependency";
        public const string SetAttributes = "SetAttributes";
        public const string Aggregate = "Aggregate";
        public const string Clear = "Clear";
        public const string ClearAsync = "ClearAsync";
        public const string Contains = "Contains";
        public const string Dispose = "Dispose";
        public const string ExecuteNonQuery = "ExecuteNonQuery";
        public const string ExecuteReader = " ExecuteReader";
        public const string ExecuteReaderCQ = "ExecuteReaderCQ";
        public const string ExecuteTask = "ExecuteTask";
        public const string GET = "get";
        public const string GetBulk = "GetBulk";
        public const string GetCacheItem = "GetCacheItem";
        public const string GetCacheStream = "GetCacheStream";
        public const string GetEnumerator = "GetEnumerator";
        public const string GetIfNewer = "GetIfNewer";
        public const string Invoke = "Invoke";
        public const string Lock = "Lock";
        public const string RegisterCQ = "RegisterCQ";
        public const string RemoveAsync = "RemoveAsync";
        public const string Search = "Search";
        public const string SearchCQ = "SearchCQ";
        public const string SearchEntries = "SearchEntries";
        public const string Unlock = "Unlock";
        public const string UnRegisterCQ = "UnRegisterCQ";
        public const string CloseStream = "CloseStream";
        public const string DisposeReader = "DisposeReader";
        public const string Count = "Count";
        public const string RaiseCustomEvent = "RaiseCustomEvent";
        public const string GetNextChunk = "GetNextChunk";
        public const string GetGroupNextChunk = "GetGroupNextChunk";
        public const string GetRunningTasks = "GetRunningTasks";
        public const string GetTaskResult = "GetTaskResult";
        public const string GetConnectedClientList = "GetConnectedClientList";
        public const string RegisterKeyNotificationCallback = "RegisterKeyNotificationCallback";
        public const string UnRegisterKeyNotificationCallback = "UnRegisterKeyNotificationCallback";
        public const string UnRegisterCacheNotification = "UnRegisterCacheNotification";
        public const string RegisterCacheNotification = "RegisterCacheNotification";
        public const string GetReaderChunkCommand = "GetReaderChunkCommand";
        public const string Touch = "TouchCommand";
        public const string Ping = "PingCommand";
        public const string Poll = "PollCommand";
        public const string CreateTopic = "CreateTopic";
        public const string GetTopic = "GetTopic";
        public const string DeleteTopic = "DeleteTopic";
        public const string CreateTopicSubscriptoin = "CreateSubscription";
        public const string PublishMessageOnTopic = "Publish";
        public const string GetTopicMessages = "GetAssingedMessages";
        public const string AcknowledgeTopicMessages = "AacknowledgeMessages";
        public const string UnSubscribe = "UnSubscribe";
        public const string MessageCount = "MessageCount";

        #region Distributed List
        public const string ListAddWithoutIndex= "ListAddWithoutIndex";
        public const string ListAddWithPivot = "ListAddWithPivot";
        public const string ListGetBulk = "ListGetBulk";
        public const string ListRemoveWithoutIndex = "ListRemoveWithoutIndex";
        public const string ListRemoveWithIndex = "ListRemoveWithIndex";
        public const string ListInsertWithIndex = "ListInsertWithIndex";
        public const string ListSearch = "ListSearch";
        public const string ListGetItem = "ListGetItem";
        public const string CollectionNotificationRegistration = "CollectionNotificationRegistration";
        #endregion

        #region Distributed Dictionary 
        public const string DictionaryAdd = "DictionaryAdd";
        public const string DictionaryGet = "DictionaryGet";
        public const string DictionaryContains = "DictionaryContains";
        public const string DictionaryRemove = "DictionaryRemove";


        #endregion

        #region Counter

        public const string IncrementByCounter = "incrementbycounter";
        public const string DecrementByCounter = "decrementbycounter";
        public const string SetValueCounter = "setvaluecounter";

        #endregion

        #region HashSet

        public const string AddHashSet = "hashsetadd";
        public const string ContainsHashSet = "hashsetcontains";
        public const string GetRandomHashSet = "hashsetgetrandom";
        public const string RemoveHashSet = "hashsetremove";
        public const string RemoveRandomHashSet = "hashsetremoverandom";

        #endregion

        #region  DataType Management Methods
        public const string CreateCounter = "CreateCounter";
        public const string CreateDictionary = "CreateDictionary";
        public const string CreateHashSet = "CreateHashSet";
        public const string CreateList = "CreateList";
        public const string CreateNotifications = "CreateNotifications";
        public const string CreateQueue = "CreateQueue";
        public const string GetCounter = "GetCounter";
        public const string GetDictionary = "GetDictionary";
        public const string GetHashSet = "GetHashSet";
        public const string GetList = "GetList";
        public const string GetNotifications = "GetNotifications";
        public const string GetQueue = "GetQueue";

        public const string DataTypeCount = "Count";
        public const string DataTypeClear = "Clear";
        public const string DataTypeGetChunk = "GetChunk";

        public const string List = "List";
        public const string Queue = "Queue";
        public const string Counter = "Counter";
        public const string HashSet = "HashSet";
        public const string Dictionary = "Dictionary";

        public const string QueueDequeue = "QueueDequeue";
        public const string QueueEnqueue = "QueueEnqueue";
        public const string QueuePeek = "QueuePeek";
        public const string QueueContains = "QueueContains";
        #endregion
    }
}
