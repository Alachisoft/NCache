// Copyright (c) 2017 Alachisoft
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

using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Logger;

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

		/// <summary>
		/// Increment the performance counter for Cache misses per second. 
		/// </summary>
		void IncrementMissPerSecStats();

        void IncrementHitsRatioPerSecStats();

        void IncrementHitsRatioPerSecBaseStats();

		/// <summary> 
		/// Increment the performance counter for Cache get operations per second. 
		/// </summary>
		void IncrementGetPerSecStats();

		/// <summary> 
		/// Increment the performance counter for Cache add operations per second. 
		/// </summary>
		void IncrementAddPerSecStats();

		/// <summary> 
		/// Increment the performance counter for Cache update operations per second. 
		/// </summary>
		void IncrementUpdPerSecStats();

		/// <summary> Increment the performance counter for Cache remove operations per second. </summary>
		void IncrementDelPerSecStats();

        /// <summary> Increment the performance counter by value for Cache remove operations per second. </summary>
        void IncrementDelPerSecStats(int value);

        /// <summary> Increment the performance counter for Cache evictions per second. </summary>
        void IncrementEvictPerSecStats();

        /// <summary> Increment the performance counter for Cache expirations per second. </summary>
        void IncrementExpiryPerSecStats();

      
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

        /// <summary> Increment the performance counter for Data Balance per second. </summary>
        void IncrementDataBalPerSecStats();

        /// <summary> Increment the performance counter for Data Balance per second by given value. </summary>
        void IncrementDataBalPerSecStatsBy(long value);


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
        void IncrementExpirationIndexSizeBy(long value);
        void IncrementExpirationIndexSize();        

        void SetQueryPerSec(long value);
        void IncrementQueryPerSecBy(long value);
        void IncrementQueryPerSec();

        void MsecPerQueryExecutionTimeBeginSample();
        void MsecPerQueryExecutionTimeEndSample();


        void IncrementAvgQuerySize(long value);


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

    }
}
