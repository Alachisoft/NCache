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
namespace Alachisoft.NCache.SocketServer.Statistics
{
    public interface StatisticsCounter
    {
        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        string InstanceName
        {
            get;
            set;
        }

        long ConncetedClients
        {
            get;
            set;
        }

        long RequestLogCount
        {
            set;
        }
        long RequestLogSize
        {
            set;
        }
        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        void InitializePerfCounters();
        /// <summary> 
        /// Increment the performance counter for Client bytes sent. 
        /// </summary>
        void IncrementBytesSentPerSecStats(long bytesSent);

        /// <summary> 
        /// Increment the performance counter for Client bytes received. 
        /// </summary>
        void IncrementBytesReceivedPerSecStats(long bytesReceived);

        /// <summary> 
        /// Increment the performance counter for Requests Per second. 
        /// </summary>
        void IncrementRequestsPerSecStats(long requests);

        /// <summary> 
        /// Increment the performance counter for Responses Per second. 
        /// </summary>
        void IncrementResponsesPerSecStats(long responses);

        void IncrementResponsesQueueCountStats();

        void IncrementResponsesQueueSizeStats(long size);

        void DecrementResponsesQueueCountStats();

        void DecrementResponsesQueueSizeStats(long size);

        /// <summary>
        ///Event Queue is maintained for each client this counter shows queue count. 
        /// </summary>
        /// 
        void IncrementEventQueueCountStats();

        void DecrementEventQueueCountStats();

        void SetEventQueueCountStats(long count);


        /// <summary>
        /// Timestamps the start of sampling interval for avg. and max. per mill-second time of 
        /// all cache operations. 
        /// </summary>
        void MsecPerCacheOperationBeginSample();

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// any operation operation.
        /// </summary>
        void MsecPerCacheOperationEndSample();

        /// <summary> 
        /// Increment the Number of client requests logged on server. 
        /// </summary>
        void IncrementRequestLogCount(long requests);
        /// <summary> 
        /// Decrement the Number of client requests logged on server. 
        /// </summary>
        void DecrementRequestLogCount();
        void DecrementRequestLogCount(long value);
        void IncrementRequestLogPerSec();

        //Bulk Counters
        void IncrementMsecPerAddBulkAvg(long value);
        void IncrementMsecPerGetBulkAvg(long value);


        double GetCounterValue(string counterName);

        void IncrementMsecPerUpdBulkAvg(long value);
        void IncrementMsecPerDelBulkAvg(long value);       

    }
}
