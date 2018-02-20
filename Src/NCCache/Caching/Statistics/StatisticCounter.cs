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

using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Caching.Statistics
{
    public interface StatisticCounter
    {
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
        string InstanceName
        {
            get;
            set;
        }


		/// <summary>
		/// Returns true if the current user has the rights to read/write to performance counters
		/// under the category of object cache.
		/// </summary>
        bool UserHasAccessRights
        {
            get;
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
		/// Increment the performance counter for Cache item count by one. 
		/// </summary>
		void IncrementCountStats(long count);

        /// <summary> 
        /// Increment the performance counter for Cache item count by one. 
        /// </summary>
        void IncrementCacheLastAccessCountStats(long count);

		/// <summary> 
		/// Increment the performance counter for Cache item count by one. 
		/// </summary>
		void IncrementCountStatsBy(long count);

		/// <summary> 
		/// Increment the performance counter for Cache hits per second. 
		/// </summary>
		void IncrementHitsPerSecStats();
        void IncrementByHitsPerSecStats(long value);

		/// <summary>
		/// Increment the performance counter for Cache misses per second. 
		/// </summary>
		void IncrementMissPerSecStats();
        void IncrementByMissPerSecStats(long value);

        void IncrementHitsRatioPerSecStats();

        void IncrementHitsRatioPerSecBaseStats();

        void IncrementByHitsRatioPerSecStats(long value);

        void IncrementByHitsRatioPerSecBaseStats(long value);
		/// <summary> 
		/// Increment the performance counter for Cache get operations per second. 
		/// </summary>
		void IncrementGetPerSecStats();
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

        /// <summary> Increment the performance counter by value for Cache remove operations per second. </summary>
        void IncrementDelPerSecStats(int value);

        /// <summary> Increment the performance counter for Cache evictions per second. </summary>
        void IncrementEvictPerSecStats();

        /// <summary> Increment the performance counter for Cache expirations per second. </summary>
        void IncrementExpiryPerSecStats();

        /// <summary>
        /// Increment the performance counter for read thru operation per sec
        /// </summary>
        void IncrementReadThruPerSec();

        /// <summary>
        /// Increment the performance counter for read thru operation per sec by given amount
        /// </summary>
        void IncrementReadThruPerSecBy(long value);

        /// <summary>
        /// Increment the performance counter for write thru operation per sec
        /// </summary>
        void IncrementWriteThruPerSec();

        /// <summary>
        /// Increment the performance counter for write thru operation per sec by given amount
        /// </summary>
        void IncrementWriteThruPerSecBy(long value);

        /// <summary> Increments the performance counter for Cache evictions per second by given value. </summary>
        void IncrementEvictPerSecStatsBy(long value);

        /// <summary> Increment the performance counter for Cache expirations per second by given value. </summary>
        void IncrementExpiryPerSecStatsBy(long value);

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per micro-second time of 
		/// fetch operations. 
		/// </summary>
		void MsecPerGetBeginSample();

		/// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per micro-second time of 
		/// fetch operations. 
		/// </summary>
		void MsecPerGetEndSample();

		/// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per micro-second time of 
		/// add operations. 
		/// </summary>
        void MsecPerAddBeginSample();

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per micro-second time of 
		/// add operations. 
		/// </summary>
		void MsecPerAddEndSample();

		/// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per micro-second time of 
		/// update operations. 
		/// </summary>
		void MsecPerUpdBeginSample();

		/// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per micro-second time of 
		/// update operations. 
		/// </summary>
		void MsecPerUpdEndSample();

		/// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per micro-second time of 
		/// remove operations. 
		/// </summary>
		void MsecPerDelBeginSample();

		/// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per micro-second time of 
		/// remove operations. 
		/// </summary>
		void MsecPerDelEndSample();

        /// <summary> Increment the performance counter for State Txfr's  per second. </summary>
        void IncrementStateTxfrPerSecStats();

        /// <summary> Increment the performance counter for State Txfr's per second by given value. </summary>
        void IncrementStateTxfrPerSecStatsBy(long value);
        /// <summary>
        /// Increment the performance counter for write behind queue operation per sec by given value
        /// </summary>
        void SetWBQueueCounter(long value);
        /// <summary>
        /// Count of items that are expired till now
        /// </summary>
        float Expirations
        {
            get;
        }

        /// <summary>
        /// Count of items that are evicted till now
        /// </summary>
        float Eviction
        {
            get;
        }

        /// <summary>
        /// Count of items that are state transfered till now
        /// </summary>
        float StateXfer
        {
            get;
        }

        /// <summary>
        /// Count of items that are balanced till now
        /// </summary>
        float DataBalance
        {
            get;
        }

        /// <summary> 
        /// Increment the performance counter for Mirror Queue size by one. 
        /// </summary>
        void IncrementMirrorQueueSizeStats(long count);

        /// <summary> 
        /// Increment the performance counter for SlidingIndex Queue size by one. 
        /// </summary>
        void IncrementSlidingIndexQueueSizeStats(long count);

        ILogger NCacheLog { get; set;}

        void IncrementWriteBehindPerSec();
        void IncrementWriteBehindPerSecBy(long value);

        void SetWBFailureRetryCounter(long value);
        void decrementWBFailureRetryCounter();

        void IncrementWBEvictionRate();

        void IncrementDSFailedOpsPerSec();

        void IncrementDSFailedOpsPerSecBy(long value);

        void IncrementDSUpdatePerSec();

        void IncrementDSUpdatePerSecBy(long p);

        void MsecPerDSWriteBeginSample();

        void MsecPerDSWriteEndSample();

        void MsecPerDSWriteEndSample(long bulkCount);

        void MsecPerDSUpdBeginSample();

        void MsecPerDSUpdEndSample();

        void MsecPerDSUpdEndSample(long bulkCount);

        void SetWBCurrentBatchOpsCounter(long value);

        void IncrementCacheSizeBy(long value);
        void IncrementCacheSize();
        void SetCacheSize(long value);

        void SetQueryIndexSize(long value);
        void IncrementQueryIndexSizeBy(long value);
        void IncrementQueryIndexSize();

        void SetEvictionIndexSize(long value);
        void IncrementEvictionIndexSizeBy(long value);
        void IncrementEvictionIndexSize();

        void SetExpirationIndexSize(long value);

        void SetGroupIndexSize(long value);
        void IncrementGroupIndexSizeBy(long value);
        void IncrementGroupIndexSize();

        void SetQueryPerSec(long value);
        void IncrementQueryPerSecBy(long value);
        void IncrementQueryPerSec();

        void MsecPerQueryExecutionTimeBeginSample();
        void MsecPerQueryExecutionTimeEndSample();

    
        void IncrementAvgQuerySize(long value);

        //MapReduce Methods
        void WaitingTasksCount(long value);
        void IncrementWaitingTasks();
        void DecrementWaitingTasks();
        void IncrementWaitingTasksBy(long value);

        void RunningTasksCount(long value);
        void IncrementRunningTasks();
        void DecrementRunningTasks();
        void IncrementRunningTasksBy(long value);

        void MappedPerSec(long value);
        void IncrementMappedPerSecRate();

        void CombinedPerSec(long value);
        void IncrementCombinedPerSecRate();

        void ReducedPerSec(long value);
        void IncrementReducedPerSecRate();

        void IncrementRunningReaders();
        void DecrementRunningReaders();
        void IncrementMsecPerMessagePublish(long count);
        void PubSubCounterSetter(string counterType, PerformanceOperator counterOperation);
        void SetPubSubCounter(long value, string counterType);
        void IncrementMessagePublishedPerSec();
        void IncrementMessageDeliverPerSec();
        void IncrementMessageDeliverPerSec(long value);
        void IncrementMessageExpiredPerSec(long value);
    }
}
