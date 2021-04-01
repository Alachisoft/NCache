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

namespace Alachisoft.NCache.Management.Statistics
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

        /// <summary>
        /// Creates Instancename 
        /// For outproc instanceName = CacheID
        /// For inProc instanceNAme = CacheID +"-" + ProcessID + ":" +port
        /// </summary>
        /// <param name="name"></param>
        /// <param name="port"></param>
        /// <param name="inProc"></param>
        /// <returns></returns>
        string GetInstanceName(string instanceName, int port, bool inProc);

        /// <summary>
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        bool UserHasAccessRights
        {
            get;
        }

        /// <summary>
        ///  Gets or Sets the value indicating whether Performance Stats collection is enabled or not.
        ///  On initialize Performance Collection is enabled.
        ///  </summary>
        bool IsEnabled
        {
            get;
            set;
        }

        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        void Dispose();

        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        void InitializePerfCounters(bool inproc);

        #endregion     

        /// <summary> 
        /// Increment the performance counter for Requests Per second by client. 
        /// </summary>
        void IncrementClientRequestsPerSecStats(long requests);

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by client. 
        /// </summary>
        void IncrementClientResponsesPerSecStats(long responses);

        /// <summary> 
        /// Increment the performance counter for Requests Per second by all the clients.
        /// </summary>
        void IncrementTotalClientRequestsPerSecStats(long requests);

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        void IncrementTotalClientResponsesPerSecStats(long responses);
        
        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        void IncrementGetPerSecStats();

        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        void IncrementByGetPerSecStats(long value);

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        void IncrementAddPerSecStats();

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        void IncrementByAddPerSecStats(long value);

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        void IncrementUpdPerSecStats();

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        void IncrementByUpdPerSecStats(long value);

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        void IncrementDelPerSecStats();

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        void IncrementByDelPerSecStats(long value);

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// event operations. 
        /// </summary>s
        void IncrementAvgEventProcessingSample(long value);

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        void IncrementEventsProcessedPerSec();

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        void IncrementEventsTriggeredPerSeconds();
        

        /// <summary>
        /// Increments the performance counter for Average �s/serialization.
        /// </summary>
        /// <param name="value">Value to increment by</param>
        void IncrementMsecPerSerialization(long value);


        /// <summary>
        /// Increments the performance counter for Average �s/serialization.
        /// </summary>
        /// <param name="value">Value to increment by</param>
        void IncrementMsecPerDeserialization(long value);

        /// <summary>
        /// Increments the performance counter for Average Item Size.
        /// </summary>
        void IncrementAvgItemSize(long itemSize);

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// fetch operations. 
        /// </summary>
        void MsecPerGetBeginSample();

        void IncrementMsecPerGetSample(long value);

        void IncrementMsecPerGetSample(long value, long baseValue);

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// fetch operations. 
        /// </summary>
        void MsecPerGetEndSample();

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// add operations. 
        /// </summary>
        void MsecPerAddBeginSample();
        void IncrementMsecPerAddSample(long value);
        void IncrementMsecPerAddSample(long value, long baseValue);

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// add operations. 
        /// </summary>
        void MsecPerAddEndSample();

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// update operations. 
        /// </summary>
        void MsecPerUpdBeginSample();
        void IncrementMsecPerUpdSample(long value);

        void IncrementMsecPerUpdSample(long value, long baseValue);

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// update operations. 
        /// </summary>
        void MsecPerUpdEndSample();

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// remove operations. 
        /// </summary>
        void MsecPerDelBeginSample();

        void IncrementMsecPerDelSample(long value);

        void IncrementMsecPerDelSample(long value, long baseValue);

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// remove operations. 
        /// </summary>
        void MsecPerDelEndSample();

        //Bulk Counters Incrementors
        void IncrementMsecPerAddBulkSample(long value);

        void IncrementMsecPerGetBulkSample(long value);

        void IncrementMsecPerUpdBulkSample(long value);

        void IncrementMsecPerDelBulkSample(long value);


        /// <summary> 
        /// Increment the performance counter for Mirror Queue size by one. 
        /// </summary>
        void IncrementRequestQueueSizeStats();

        /// <summary> 
        /// Decrement the performance counter for Mirror Queue size by one. 
        /// </summary>
        void DecrementRequestQueueSizeStats();

        void IncrementMsecPerMessagePublish(long value);

        void IncrementMessageCounter(string counterName);

        void IncrementMessagePublishedPerSec();

        void IncrementMessageDeliverPerSec(long value);

        void StartPublishingCounters(string bindIp);
    }
}
