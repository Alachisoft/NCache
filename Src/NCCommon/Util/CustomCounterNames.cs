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
namespace Alachisoft.NCache.Common.Util
{
    public class CustomCounterNames
    {
        public const string Count = "Count";
        public const string CacheSize = "Cache Size";
        public const string AddPerSec = "Additions/sec";
        public const string UpdatePerSec = "Updates/sec";
        public const string FetchPerSec = "Fetches/sec";
        public const string HitPerSec = "Hits/sec";
        public const string MissPerSec = "Misses/sec";
        public const string DeletePerSec = "Deletes/sec";
        //Client Counters
        public const string ReadOperationsPerSec = "Read Operations/sec";
        public const string WriteOperationsPerSec = "Write Operations/sec";
        public const string EventsTriggeredPerSec = "Events Triggered/sec";
        public const string EventProcesedPerSec = "Events Proccesed/sec";

        //Bulk Counters
        public const string MsecPerAddBulkAvg = "Average us/addbulk";
        public const string MsecPerAddBulkBase = "Average us/addbulk base";
        public const string MsecPerGetBulkAvg = "Average us/fetchbulk";
        public const string MsecPerGetBulkBase = "Average us/fetchbulk base";
        public const string MsecPerUpdBulkAvg = "Average us/insertbulk";
        public const string MsecPerUpdBulkBase = "Average us/insertbulk base";
        public const string MsecPerDelBulkAvg = "Average us/removebulk";
        public const string MsecPerDelBulkBase = "Average us/removebulk base";


        public const string MsecPerGetAvg = "Average us/fetch";
        public const string MsecPerGetBase = "Average us/fetch base";
        public const string MsecPerAddAvg = "Average us/add";
        public const string MsecPerAddBase = "Average us/add base";
        public const string MsecPerUpdAvg = "Average us/insert";
        public const string MsecPerUpdBase = "Average us/insert base";
        public const string MsecPerDelAvg = "Average us/remove";
        public const string MsecPerDelBase = "Average us/remove base";
        public const string ReqrQueueSize = "Request Queue Size";
        public const string AvgItemSize = "Average Item Size";
        public const string AvgItemSizeBase = "Average Item Size base";

        public const string MsecPerEventAvg = "Average us/event";
        public const string MsecPerEventBase = "Average us/event base";

        public const string MsecPerSerializationAvg = "Average us/serialization";
        public const string MsecPerSerializationAvgBase = "Average us/serialization base";
        public const string MsecPerDeserializationAvg = "Average us/deserialization";
        public const string MsecPerDeserializationAvgBase = "Average us/deserialization base";



        public const string AvgPublishMessage = "Average µs/Published Messages";
        public const string AvgPublishMessageBase = "Average µs/Published Messages Base";
        public const string MessagePublishPerSec = "Messages Published/sec";
        public const string MessageDeliveryPerSec = "Messages Delivered/sec";

        //Client
        public const string ClientRequestsPerSec = "Client Requests/sec";
        public const string ClientResponsesPerSec = "Client Responses/sec";
        public const string TotalClientRequestsPerSec = "Client Requests/sec";
        public const string TotalClientStats = "_Total_ client stats";
        public const string TotalClientResponsesPerSec = "Client Responses/sec";

        public const string RequestsPerSec = "Requests/sec";
        public const string ResponsesPerSec = "Responses/sec";
        public const string ClientBytesSentPerSec = "Client bytes sent/sec";
        public const string ClientBytesReceiedPerSec = "Client bytes received/sec";

        public const string MsecPerCacheOperation = "Average us/cache operation";
        public const string MsecPerCacheOperationBase = "Average us/cache operation base";

        public const string ResponseQueueCount = "Response Queue Count";
        public const string ResponseQueueSize = "Response Queue Size";
        public const string EventQueueCount = "Event Queue Count";

        public const string RequestLogPerSecond = "Requests Logged/sec";
        public const string RequestLogSize = "Request Log Ledger Size";

        public const string ConnectedClients = "# Clients";
        public const string MemoryUsage = "Private Bytes";
    }

}