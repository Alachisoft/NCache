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

using System;
using System.Diagnostics;
using Alachisoft.NCache.Common.Stats;
using System.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using System.Collections.Generic;

namespace Alachisoft.NCache.Caching.Statistics
{
	/// <summary>
	/// Summary description for PerfStatsCollector.
	/// </summary>
	public class PerfStatsCollector : IDisposable, StatisticCounter
	{
        HPTime initTime;
		/// <summary> Instance name. </summary>
		private string					_instanceName;
        /// <summary> Port number. </summary>
        private string                     _port;
        /// <summary>
        /// whether extended cache monitoring is enabled or not.
        /// </summary>
        private bool _enableExtendedMonitoring;

		/// <summary> performance counter for Cache item count. </summary>
		private PerformanceCounter 		_pcCount = null;
        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcCachelastAccessCount = null;
		/// <summary> performance counter for Cache hits per second. </summary>
		private PerformanceCounter 		_pcHitsPerSec = null;
		/// <summary> performance counter for Cache misses per second. </summary>
		private PerformanceCounter 		_pcMissPerSec = null;
        private PerformanceCounter      _pcHitsRatioSec = null;
        private PerformanceCounter      _pcHitsRatioSecBase = null;
		/// <summary> performance counter for Cache get operations per second. </summary>
		private PerformanceCounter 		_pcGetPerSec = null;
		/// <summary> performance counter for Cache add operations per second. </summary>
		private PerformanceCounter		_pcAddPerSec = null;
		/// <summary> performance counter for Cache update operations per second. </summary>
		private PerformanceCounter 		_pcUpdPerSec = null;
		/// <summary> performance counter for Cache remove operations per second. </summary>
		private PerformanceCounter 		_pcDelPerSec = null;
		/// <summary> performance counter for Cache evictions per second. </summary>
		private PerformanceCounter 		_pcEvictPerSec = null;
        /// <summary> performance counter for Cache expirations per second. </summary>
        private PerformanceCounter      _pcExpiryPerSec = null;
        /// <summary> performance counter for State Txfr's per second. </summary>
        private PerformanceCounter      _pcStateTxfrPerSec = null;

        /// <summary> performance counter for Mirror Queue size. </summary>
        private PerformanceCounter      _pcMirrorQueueSize = null;
        private PerformanceCounter      _pcSlidingIndexQueueSize = null;

		/// <summary> performance counter for Cache avg. per milli-second time of get operations. </summary>
		private PerformanceCounter 		_pcMsecPerGetAvg = null;
		/// <summary> performance counter for Cache avg. per milli-second time of add operations. </summary>
		private PerformanceCounter		_pcMsecPerAddAvg = null;
		/// <summary> performance counter for Cache avg. per milli-second time of update operations. </summary>
		private PerformanceCounter 		_pcMsecPerUpdAvg = null;
		/// <summary> performance counter for Cache avg. per milli-second time of remove operations. </summary>
		private PerformanceCounter 		_pcMsecPerDelAvg = null;
		/// <summary> performance counter for Cache max. per milli-second time of get operations. </summary>
		private PerformanceCounter 		_pcMsecPerGetBase = null;
		/// <summary> performance counter for Cache max. per milli-second time of add operations. </summary>
		private PerformanceCounter		_pcMsecPerAddBase = null;
		/// <summary> performance counter for Cache max. per milli-second time of update operations. </summary>
		private PerformanceCounter 		_pcMsecPerUpdBase = null;
		/// <summary> performance counter for Cache max. per milli-second time of remove operations. </summary>
		private PerformanceCounter 		_pcMsecPerDelBase = null;

        /// <summary> performance counter for read thru per sec</summary>
        private PerformanceCounter      _pcReadThruPerSec = null;
        /// <summary> performance counter for write thru per sec</summary>
        private PerformanceCounter      _pcWriteThruPerSec = null;

		/// <summary> usage statistics for Cache per milli-second time of get operations. </summary>
		private UsageStats 				_usMsecPerGet = null;
		/// <summary> usage statistics for Cache per milli-second time of add operations. </summary>
		private UsageStats				_usMsecPerAdd = null;
		/// <summary> usage statistics for Cache per milli-second time of update operations. </summary>
		private UsageStats 				_usMsecPerUpd = null;
		/// <summary> usage statistics for Cache per milli-second time of remove operations. </summary>
		private UsageStats 				_usMsecPerDel = null;

        /// <summary>performance counter for Size of the cache including store and all indices.. </summary>
        private PerformanceCounter _pcCacheSize = null;
        /// <summary> performance counter for Size of query indices define on cache. </summary>
        private PerformanceCounter _pcQueryIndexSize = null;
        /// <summary> performance counter for Size of expiration indices define on cache. </summary>
        private PerformanceCounter _pcExpirationIndexSize = null;
        /// <summary> performance counter for Size of eviction indices define on cache. </summary>
        private PerformanceCounter _pcEvictionIndexSize = null;
        /// <summary> performance counter for Size of group and sub group indices.. </summary>
        private PerformanceCounter _pcGroupIndexSize = null;
        /// <summary> performance counter for Number of queries per sec on cache. </summary>
        private PerformanceCounter _pcQueryPerSec = null;
        /// <summary> performance counter for Average time query take while executing.</summary>
        private PerformanceCounter _pcAvgQueryExecutionTime = null;
        private PerformanceCounter _pcAvgQueryExecutionTimeBase = null;
        private UsageStats _usMsecPerQueryExecution = null;
        /// <summary> performance counter for average Number of items returned by queries. </summary>
        private PerformanceCounter _pcAvgQuerySize = null;
        private PerformanceCounter _pcAvgQuerySizeBase = null;
        private UsageStats _usMsecPerQuerySize = null;

        ///<summary> stats for Write behind queue count</summary>
        private PerformanceCounter _pcWBQueueCount = null;
        private PerformanceCounter _pcWriteBehindPerSec = null;
        private UsageStats _usMsecPerDSWrite = null;
        private PerformanceCounter _pcMsecPerDSWriteAvg= null;
        private PerformanceCounter _pcMsecPerDSWriteBase = null;
        private PerformanceCounter _pcWBFailureRertyCount = null;
        private PerformanceCounter _pcWBEvicPerSec = null;
        private PerformanceCounter _pcDSUpdatesPerSec = null;
        private PerformanceCounter _pcMsecPerDSUpdateAvg = null;
        private PerformanceCounter _pcMsecPerDSUpdateBase = null;
        private UsageStats _usMsecPerDSUp = null;
        private PerformanceCounter _pcDSFailedOpsPerSec = null;
        private PerformanceCounter _pcWBCurrenBatchOpCount = null;

        private PerformanceCounter _mrRunningTasks = null;
        private PerformanceCounter _mrWaitingTasks = null;
        private PerformanceCounter _mrMapperPerSec = null;
        private PerformanceCounter _mrCombinedPerSec = null;
        private PerformanceCounter _mrReducedPerSec = null;

        private PerformanceCounter _runningReader = null;

        #region Pub-Sub
        /// <summary> performance counter for number of messages in cache. </summary>
        private PerformanceCounter _pcMessageCount;
        
        /// <summary> performance counter for number of topics in cache. </summary>
        private PerformanceCounter _pcTopicCount;
        private PerformanceCounter _pcMessageStoreSize;

        private PerformanceCounter _pcMessagePublishPerSec;
        private PerformanceCounter _pcMessageDeliverPerSec;
        private PerformanceCounter _pcMessageExpiredPerSec;

        private Dictionary<string, PerformanceCounter> _pubsubCounterList;

        #endregion

        //public NewTrace nTrace;
        private ILogger _ncacheLog;

		/// <summary> Category name of counter performance data.</summary>
        /// 
        private const string PC_CATEGORY = "NCache";
        private float _expirations = 0;
        private float _evictions = 0;
        private float _stateXfer = 0;
        private float _dataBalance = 0;
       

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        public PerfStatsCollector(string instanceName, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, 0, inProc);

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public PerfStatsCollector(string instanceName, int port, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, port, inProc);
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
        public string GetInstanceName(string instanceName, int port, bool inProc)
        {
            // This will not be replaced with ServiceConfiguration as this if for DEV only and loads from NCHOST config
            if (System.Configuration.ConfigurationSettings.AppSettings["InstanceNameText"] != null)
                instanceName = System.Configuration.ConfigurationSettings.AppSettings["InstanceNameText"] + "_" + instanceName;
 
            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
        }

		/// <summary>
		/// Returns true if the current user has the rights to read/write to performance counters
		/// under the category of object cache.
		/// </summary>
		public string InstanceName
		{
			get { return _instanceName; }
			set { _instanceName = value; }
		}


		/// <summary>
		/// Returns true if the current user has the rights to read/write to performance counters
		/// under the category of object cache.
		/// </summary>
		public bool UserHasAccessRights
		{
			get
			{
				try
				{
					PerformanceCounterPermission permissions = new
						PerformanceCounterPermission(PerformanceCounterPermissionAccess.Instrument,
						".", PC_CATEGORY);
					permissions.Demand();

					if(!PerformanceCounterCategory.Exists(PC_CATEGORY, "."))
					{
						return false;
					}
				}
				catch(Exception e)
				{
					if(NCacheLog.IsInfoEnabled) NCacheLog.Info("PerfStatsCollector.UserHasAccessRights", e.Message);
					return false;
				}
				return true;
			}
		}



		#region	/                 --- IDisposable ---           /

		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or 
		/// resetting unmanaged resources.
		/// </summary>
		public void Dispose()
		{
			lock(this)
			{
				if(_pcCount != null)
				{
					_pcCount.RemoveInstance();
					_pcCount.Dispose();
					_pcCount = null;
				}
                if (_pcCachelastAccessCount != null)
                {
                    _pcCachelastAccessCount.RemoveInstance();
                    _pcCachelastAccessCount.Dispose();
                    _pcCachelastAccessCount = null;
                }
				if(_pcHitsPerSec != null)
				{
					_pcHitsPerSec.RemoveInstance();
					_pcHitsPerSec.Dispose();
					_pcHitsPerSec = null;
				}
				if(_pcMissPerSec != null)
				{
					_pcMissPerSec.RemoveInstance();
					_pcMissPerSec.Dispose();
					_pcMissPerSec = null;
				}
                if (_pcHitsRatioSec != null)
                {
                    _pcHitsRatioSec.RemoveInstance();
                    _pcHitsRatioSec.Dispose();
                    _pcHitsRatioSec = null;
                }
                if (_pcHitsRatioSecBase != null)
                {
                    _pcHitsRatioSecBase.RemoveInstance();
                    _pcHitsRatioSecBase.Dispose();
                    _pcHitsRatioSecBase = null;
                }
				if(_pcAddPerSec != null)
				{
					_pcAddPerSec.RemoveInstance();
					_pcAddPerSec.Dispose();
					_pcAddPerSec = null;
				}
				if(_pcGetPerSec != null)
				{
					_pcGetPerSec.RemoveInstance();
					_pcGetPerSec.Dispose();
					_pcGetPerSec = null;
				}
				if(_pcUpdPerSec != null)
				{
					_pcUpdPerSec.RemoveInstance();
					_pcUpdPerSec.Dispose();
					_pcUpdPerSec = null;
				}
				if(_pcDelPerSec != null)
				{
					_pcDelPerSec.RemoveInstance();
					_pcDelPerSec.Dispose();
					_pcDelPerSec = null;
				}
				if(_pcMsecPerAddAvg != null)
				{
					_pcMsecPerAddAvg.RemoveInstance();
					_pcMsecPerAddAvg.Dispose();
					_pcMsecPerAddAvg = null;
				}
				if(_pcMsecPerDelAvg != null)
				{
					_pcMsecPerDelAvg.RemoveInstance();
					_pcMsecPerDelAvg.Dispose();
					_pcMsecPerDelAvg = null;
				}
				if(_pcMsecPerGetAvg != null)
				{
					_pcMsecPerGetAvg.RemoveInstance();
					_pcMsecPerGetAvg.Dispose();
					_pcMsecPerGetAvg = null;
				}
				if(_pcMsecPerUpdAvg != null)
				{
					_pcMsecPerUpdAvg.RemoveInstance();
					_pcMsecPerUpdAvg.Dispose();
					_pcMsecPerUpdAvg = null;
				}
				if(_pcMsecPerAddBase != null)
				{
					_pcMsecPerAddBase.RemoveInstance();
					_pcMsecPerAddBase.Dispose();
					_pcMsecPerAddBase = null;
				}
				if(_pcMsecPerDelBase != null)
				{
					_pcMsecPerDelBase.RemoveInstance();
					_pcMsecPerDelBase.Dispose();
					_pcMsecPerDelBase = null;
				}
				if(_pcMsecPerGetBase != null)
				{
					_pcMsecPerGetBase.RemoveInstance();
					_pcMsecPerGetBase.Dispose();
					_pcMsecPerGetBase = null;
				}
				if(_pcMsecPerUpdBase != null)
				{
					_pcMsecPerUpdBase.RemoveInstance();
					_pcMsecPerUpdBase.Dispose();
					_pcMsecPerUpdBase = null;
				}
				if(_pcEvictPerSec != null)
				{
					_pcEvictPerSec.RemoveInstance();
					_pcEvictPerSec.Dispose();
					_pcEvictPerSec = null;
				}
				if(_pcExpiryPerSec != null)
				{
					_pcExpiryPerSec.RemoveInstance();
					_pcExpiryPerSec.Dispose();
					_pcExpiryPerSec = null;
				}
                if (_pcExpiryPerSec != null)
                {
                    _pcExpiryPerSec.RemoveInstance();
                    _pcExpiryPerSec.Dispose();
                    _pcExpiryPerSec = null;
                }
                if (_pcStateTxfrPerSec != null)
                {
                    _pcStateTxfrPerSec.RemoveInstance();
                    _pcStateTxfrPerSec.Dispose();
                    _pcStateTxfrPerSec = null;
                }
               
                if (_pcMirrorQueueSize != null)
                {
                    _pcMirrorQueueSize.RemoveInstance();
                    _pcMirrorQueueSize.Dispose();
                    _pcMirrorQueueSize = null;
                }
                if (_pcSlidingIndexQueueSize != null)
                {
                    _pcSlidingIndexQueueSize.RemoveInstance();
                    _pcSlidingIndexQueueSize.Dispose();
                    _pcSlidingIndexQueueSize = null;
                }

                if (_pcCacheSize != null)
                {
                    _pcCacheSize.RemoveInstance();
                    _pcCacheSize.Dispose();
                    _pcCacheSize = null;
                }
                if (_pcQueryIndexSize != null)
                {
                    _pcQueryIndexSize.RemoveInstance();
                    _pcQueryIndexSize.Dispose();
                    _pcQueryIndexSize = null;
                }
                if (_pcEvictionIndexSize != null)
                {
                    _pcEvictionIndexSize.RemoveInstance();
                    _pcEvictionIndexSize.Dispose();
                    _pcEvictionIndexSize = null;
                }
                if (_pcExpirationIndexSize != null)
                {
                    _pcExpirationIndexSize.RemoveInstance();
                    _pcExpirationIndexSize.Dispose();
                    _pcExpirationIndexSize = null;
                }
                if (_pcGroupIndexSize != null)
                {
                    _pcGroupIndexSize.RemoveInstance();
                    _pcGroupIndexSize.Dispose();
                    _pcGroupIndexSize = null;
                }
                if (_pcQueryPerSec != null)
                {
                    _pcQueryPerSec.RemoveInstance();
                    _pcQueryPerSec.Dispose();
                    _pcQueryPerSec = null;
                }

                if (_pcAvgQuerySize != null)
                {
                    _pcAvgQuerySize.RemoveInstance();
                    _pcAvgQuerySize.Dispose();
                    _pcAvgQuerySize = null;
                }

                if (_pcAvgQuerySizeBase != null)
                {
                    _pcAvgQuerySizeBase.RemoveInstance();
                    _pcAvgQuerySizeBase.Dispose();
                    _pcAvgQuerySizeBase = null;
                }
                if (_pcAvgQueryExecutionTime != null)
                {
                    _pcAvgQueryExecutionTime.RemoveInstance();
                    _pcAvgQueryExecutionTime.Dispose();
                    _pcAvgQueryExecutionTime = null;
                }


                if (this._pcReadThruPerSec != null)
                {
                    this._pcReadThruPerSec.RemoveInstance();
                    this._pcReadThruPerSec.Dispose();
                    this._pcReadThruPerSec = null;
                }
                if (this._pcWriteThruPerSec != null)
                {
                    this._pcWriteThruPerSec.RemoveInstance();
                    this._pcWriteThruPerSec.Dispose();
                    this._pcWriteThruPerSec = null;
                }
                if (_pcWBQueueCount != null)
                {
                    _pcWBQueueCount.RemoveInstance();
                    _pcWBQueueCount.Dispose();
                    _pcWBQueueCount = null;
                }

                if (_pcWBCurrenBatchOpCount != null)
                {
                    _pcWBCurrenBatchOpCount.RemoveInstance();
                    _pcWBCurrenBatchOpCount.Dispose();
                    _pcWBCurrenBatchOpCount = null;
                }

                if (_pcAvgQueryExecutionTimeBase != null)
                {
                    _pcAvgQueryExecutionTimeBase.RemoveInstance();
                    _pcAvgQueryExecutionTimeBase.Dispose();
                    _pcAvgQueryExecutionTimeBase = null;
                }

                if (_pcWriteBehindPerSec != null)
                {
                    _pcWriteBehindPerSec.RemoveInstance();
                    _pcWriteBehindPerSec.Dispose();
                    _pcWriteBehindPerSec = null;
                }

                if (_pcMsecPerDSWriteAvg != null)
                {
                    _pcMsecPerDSWriteAvg.RemoveInstance();
                    _pcMsecPerDSWriteAvg.Dispose();
                    _pcMsecPerDSWriteAvg = null;
                }

                if (_pcMsecPerDSWriteBase != null)
                {
                    _pcMsecPerDSWriteBase.RemoveInstance();
                    _pcMsecPerDSWriteBase.Dispose();
                    _pcMsecPerDSWriteBase = null;
                }

                if (_pcWBFailureRertyCount != null)
                {
                    _pcWBFailureRertyCount.RemoveInstance();
                    _pcWBFailureRertyCount.Dispose();
                    _pcWBFailureRertyCount = null;
                }

                if (_pcWBEvicPerSec != null)
                {
                    _pcWBEvicPerSec.RemoveInstance();
                    _pcWBEvicPerSec.Dispose();
                    _pcWBEvicPerSec = null;
                }

                if (_pcDSUpdatesPerSec != null)
                {
                    _pcDSUpdatesPerSec.RemoveInstance();
                    _pcDSUpdatesPerSec.Dispose();
                    _pcDSUpdatesPerSec = null;
                }

                if (_pcMsecPerDSUpdateAvg != null)
                {
                    _pcMsecPerDSUpdateAvg.RemoveInstance();
                    _pcMsecPerDSUpdateAvg.Dispose();
                    _pcMsecPerDSUpdateAvg = null;
                }

                if (_pcMsecPerDSUpdateBase != null)
                {
                    _pcMsecPerDSUpdateBase.RemoveInstance();
                    _pcMsecPerDSUpdateBase.Dispose();
                    _pcMsecPerDSUpdateBase = null;
                }

                if (_pcDSFailedOpsPerSec != null)
                {
                    _pcDSFailedOpsPerSec.RemoveInstance();
                    _pcDSFailedOpsPerSec.Dispose();
                    _pcDSFailedOpsPerSec = null;
                }

                if (_pcWBCurrenBatchOpCount != null)
                {
                    _pcWBCurrenBatchOpCount.RemoveInstance();
                    _pcWBCurrenBatchOpCount.Dispose();
                    _pcWBCurrenBatchOpCount = null;
                }

                if (_mrRunningTasks != null)
                {
                    _mrRunningTasks.RemoveInstance();
                    _mrRunningTasks.Dispose();
                    _mrRunningTasks = null;
                }
                if (_mrWaitingTasks != null)
                {
                    _mrWaitingTasks.RemoveInstance();
                    _mrWaitingTasks.Dispose();
                    _mrWaitingTasks = null;
                }
                if (_mrMapperPerSec != null)
                {
                    _mrMapperPerSec.RemoveInstance();
                    _mrMapperPerSec.Dispose();
                    _mrMapperPerSec = null;
                }
                if (_mrCombinedPerSec != null)
                {
                    _mrCombinedPerSec.RemoveInstance();
                    _mrCombinedPerSec.Dispose();
                    _mrCombinedPerSec = null;
                }
                if (_mrReducedPerSec != null)
                {
                    _mrReducedPerSec.RemoveInstance();
                    _mrReducedPerSec.Dispose();
                    _mrReducedPerSec = null;
                }
                if (_runningReader != null)
                {
                    _runningReader.RemoveInstance();
                    _runningReader.Dispose();
                    _runningReader = null;
                }
                if (_pubsubCounterList != null)
                {
                    foreach (PerformanceCounter counter in _pubsubCounterList.Values)
                    {
                        if (counter != null)
                        {
                            counter.RemoveInstance();
                            counter.Dispose();
                        }
                    }

                    _pubsubCounterList.Clear();
                    _pubsubCounterList = null;

                }

            }
		}


		#endregion

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Initializes the counter instances and category.
		/// </summary>
        public void InitializePerfCounters(bool inproc)
		{
            try
            {
                if (!UserHasAccessRights)
                    return;

                lock (this)
                {
                    int ncounter = 1;
                    string instname = _instanceName;

                    if (inproc)
                    {
                        int processid = Process.GetCurrentProcess().Id;
                        while (PerformanceCounterCategory.InstanceExists(instname, PC_CATEGORY))
                        {
                            instname = _instanceName + ":" + ncounter.ToString() + " - " + processid.ToString() + _port;
                            ncounter++;
                        }
                        _instanceName = instname;
                    }

                    _pcCount = new PerformanceCounter(PC_CATEGORY, "Count", _instanceName, false);
                    _pcCachelastAccessCount = new PerformanceCounter(PC_CATEGORY, "CacheLastAccessCount", _instanceName, false);
                    _pcHitsPerSec = new PerformanceCounter(PC_CATEGORY, "Hits/sec", _instanceName, false);
                    _pcMissPerSec = new PerformanceCounter(PC_CATEGORY, "Misses/sec", _instanceName, false);
                    _pcHitsRatioSec = new PerformanceCounter(PC_CATEGORY, "Hits ratio/sec (%)", _instanceName, false);
                    _pcHitsRatioSecBase = new PerformanceCounter(PC_CATEGORY, "Hits ratio/sec base", _instanceName, false);
                    _pcAddPerSec = new PerformanceCounter(PC_CATEGORY, "Additions/sec", _instanceName, false);
                    _pcGetPerSec = new PerformanceCounter(PC_CATEGORY, "Fetches/sec", _instanceName, false);
                    _pcUpdPerSec = new PerformanceCounter(PC_CATEGORY, "Updates/sec", _instanceName, false);
                    _pcDelPerSec = new PerformanceCounter(PC_CATEGORY, "Deletes/sec", _instanceName, false);
                    _pcEvictPerSec = new PerformanceCounter(PC_CATEGORY, "Evictions/sec", _instanceName, false);
                    _pcExpiryPerSec = new PerformanceCounter(PC_CATEGORY, "Expirations/sec", _instanceName, false);
                    _pcStateTxfrPerSec = new PerformanceCounter(PC_CATEGORY, "State Transfer/sec", _instanceName, false);

                    _pcMsecPerGetAvg = new PerformanceCounter(PC_CATEGORY, "Average us/fetch", _instanceName, false);
                    _pcMsecPerGetBase = new PerformanceCounter(PC_CATEGORY, "Average us/fetch base", _instanceName, false);
                    _usMsecPerGet = new UsageStats();
                    _pcMsecPerAddAvg = new PerformanceCounter(PC_CATEGORY, "Average us/add", _instanceName, false);
                    _pcMsecPerAddBase = new PerformanceCounter(PC_CATEGORY, "Average us/add base", _instanceName, false);
                    _usMsecPerAdd = new UsageStats();
                    _pcMsecPerUpdAvg = new PerformanceCounter(PC_CATEGORY, "Average us/insert", _instanceName, false);
                    _pcMsecPerUpdBase = new PerformanceCounter(PC_CATEGORY, "Average us/insert base", _instanceName, false);
                    _usMsecPerUpd = new UsageStats();
                    _pcMsecPerDelAvg = new PerformanceCounter(PC_CATEGORY, "Average us/remove", _instanceName, false);
                    _pcMsecPerDelBase = new PerformanceCounter(PC_CATEGORY, "Average us/remove base", _instanceName, false);
                    _usMsecPerDel = new UsageStats();

                    _pcMirrorQueueSize = new PerformanceCounter(PC_CATEGORY, "Mirror queue size", _instanceName, false);
                    _pcSlidingIndexQueueSize = new PerformanceCounter(PC_CATEGORY, "Sliding Index queue size", _instanceName, false);

                    _pcCacheSize = new PerformanceCounter(PC_CATEGORY, "Cache Size", _instanceName, false);
                    _pcCacheSize.RawValue = 0;
                    _pcQueryIndexSize = new PerformanceCounter(PC_CATEGORY, "Query Index Size", _instanceName, false);
                    _pcEvictionIndexSize = new PerformanceCounter(PC_CATEGORY, "Eviction Index Size", _instanceName, false);
                    _pcExpirationIndexSize = new PerformanceCounter(PC_CATEGORY, "Expiration Index Size", _instanceName, false);
                    _pcGroupIndexSize = new PerformanceCounter(PC_CATEGORY, "Group Index Size", _instanceName, false);
                    _pcQueryPerSec = new PerformanceCounter(PC_CATEGORY, "Queries/sec", _instanceName, false);

                    _pcAvgQueryExecutionTime = new PerformanceCounter(PC_CATEGORY, "Average us/Query Execution", _instanceName, false);
                    _pcAvgQueryExecutionTimeBase = new PerformanceCounter(PC_CATEGORY, "Average us/Query Execution base", _instanceName, false);
                    _usMsecPerQueryExecution = new UsageStats();
                    _pcAvgQuerySize = new PerformanceCounter(PC_CATEGORY, "Average Query Size", _instanceName, false);
                    _pcAvgQuerySizeBase = new PerformanceCounter(PC_CATEGORY, "Average Query Size base", _instanceName, false);

                    //Map Reduce Counters.
                    _mrRunningTasks = new PerformanceCounter(PC_CATEGORY, "MapReduce Running Tasks", _instanceName, false);
                    _mrWaitingTasks = new PerformanceCounter(PC_CATEGORY, "MapReduce Waiting Tasks", _instanceName, false);
                    _mrMapperPerSec = new PerformanceCounter(PC_CATEGORY, "MapReduce Mapped/sec", _instanceName, false);
                    _mrCombinedPerSec = new PerformanceCounter(PC_CATEGORY, "MapReduce Combined/sec", _instanceName, false);
                    _mrReducedPerSec = new PerformanceCounter(PC_CATEGORY, "MapReduce Reduced/sec", _instanceName, false);


                    if (ServiceConfiguration.EnableDataSourceCounters)
                    {
                        this._pcReadThruPerSec = new PerformanceCounter(PC_CATEGORY, "Read-thru/sec", _instanceName, false);
                        this._pcWriteThruPerSec = new PerformanceCounter(PC_CATEGORY, "Write-thru/sec", _instanceName, false);
                        //Write behind counters
                        _pcWBQueueCount = new PerformanceCounter(PC_CATEGORY, "Write-behind queue count", _instanceName, false);
                        _pcWriteBehindPerSec = new PerformanceCounter(PC_CATEGORY, "Write-behind/sec", _instanceName, false);
                        _pcMsecPerDSWriteAvg = new PerformanceCounter(PC_CATEGORY, "Average us/datasource write", _instanceName, false);
                        _pcMsecPerDSWriteBase = new PerformanceCounter(PC_CATEGORY, "Average us/datasource write base", _instanceName, false);
                        _usMsecPerDSWrite = new UsageStats();
                        _pcWBFailureRertyCount = new PerformanceCounter(PC_CATEGORY, "Write-behind failure retry count", _instanceName, false);
                        _pcWBEvicPerSec = new PerformanceCounter(PC_CATEGORY, "Write-behind evictions/sec", _instanceName, false);
                        _pcDSUpdatesPerSec = new PerformanceCounter(PC_CATEGORY, "Datasource updates/sec", _instanceName, false);
                        _pcMsecPerDSUpdateAvg = new PerformanceCounter(PC_CATEGORY, "Average us/datasource update", _instanceName, false);
                        _pcMsecPerDSUpdateBase = new PerformanceCounter(PC_CATEGORY, "Average us/datasource update base", _instanceName, false);
                        _usMsecPerDSUp = new UsageStats();
                        _pcDSFailedOpsPerSec = new PerformanceCounter(PC_CATEGORY, "Datasource failed operations/sec", _instanceName, false);
                        _pcWBCurrenBatchOpCount = new PerformanceCounter(PC_CATEGORY, "Current batch operations count", _instanceName, false);
                        
                    }

                    _runningReader = new PerformanceCounter(PC_CATEGORY, CounterNames.RUNNINGREADERS, _instanceName, false);
                    
                    #region Pub_Sub
                    _pubsubCounterList = new Dictionary<string, PerformanceCounter>();
                    _pcMessageCount = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageCount, _instanceName, false);
                    _pcTopicCount = new PerformanceCounter(PC_CATEGORY, CounterNames.TopicCount, _instanceName, false);
                    _pcMessageStoreSize = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageStoreSize, _instanceName, false);
                    _pcMessagePublishPerSec= new PerformanceCounter(PC_CATEGORY, CounterNames.MessagePublishPerSec, _instanceName, false);
                    _pcMessageDeliverPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageDeliveryPerSec, _instanceName, false);
                    _pcMessageExpiredPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageExpiredPerSec, _instanceName, false);
                    _pubsubCounterList.Add(CounterNames.MessageCount, _pcMessageCount);
                    _pubsubCounterList.Add(CounterNames.TopicCount, _pcTopicCount);
                    _pubsubCounterList.Add(CounterNames.MessageStoreSize, _pcMessageStoreSize);
                    _pubsubCounterList.Add(CounterNames.MessagePublishPerSec, _pcMessagePublishPerSec);
                    _pubsubCounterList.Add(CounterNames.MessageDeliveryPerSec, _pcMessageDeliverPerSec);
                    _pubsubCounterList.Add(CounterNames.MessageExpiredPerSec, _pcMessageExpiredPerSec);

                    foreach (var item in _pubsubCounterList.Values)
                    {
                        item.RawValue = 0;
                    }
                    #endregion
                    _usMsecPerDel = new UsageStats();
                }
            }
            catch (Exception e)
            {
                NCacheLog.Error("PerfStatsCollector.PerfStatsCollector()", e.Message);
            }

		}


		#endregion

		/// <summary> 
		/// Increment the performance counter for Cache item count by one. 
		/// </summary>
		public void IncrementCountStats(long count)
		{
            if (_pcCount != null)
            {
                lock (_pcCount)
                {
                    _pcCount.RawValue = count;
                }
            }
		}

        /// <summary> 
        /// Increment the performance counter for Cache item count by one. 
        /// </summary>
        public void IncrementCacheLastAccessCountStats(long count)
        {
            if (_pcCachelastAccessCount != null)
            {
                lock (_pcCachelastAccessCount)
                {
                    _pcCachelastAccessCount.RawValue = count;
                }
            }
        }

		/// <summary> 
		/// Increment the performance counter for Cache item count by one. 
		/// </summary>
		public void IncrementCountStatsBy(long count)
		{
            if (_pcCount != null)
            {
                lock (_pcCount)
                {
                    _pcCount.IncrementBy(count);
                }
            }
		}

		/// <summary> 
		/// Increment the performance counter for Cache hits per second. 
		/// </summary>
		public void IncrementHitsPerSecStats()
		{
            if (_pcHitsPerSec != null) lock (_pcHitsPerSec) { _pcHitsPerSec.Increment(); }
		}

        /// <summary> 
        /// Increment the performance counter for Cache hits per second. 
        /// </summary>
        public void IncrementByHitsPerSecStats(long value)
        {
            if (_pcHitsPerSec != null) lock (_pcHitsPerSec) { _pcHitsPerSec.IncrementBy(value); }
        }

		/// <summary>
		/// Increment the performance counter for Cache misses per second. 
		/// </summary>
		public void IncrementMissPerSecStats()
		{
            if (_pcMissPerSec != null) lock (_pcMissPerSec) { _pcMissPerSec.Increment(); }
		}

        /// <summary>
        /// Increment the performance counter for Cache misses per second. 
        /// </summary>
        public void IncrementByMissPerSecStats(long value)
        {
            if (_pcMissPerSec != null) lock (_pcMissPerSec) { _pcMissPerSec.IncrementBy(value); }
        }

        public void IncrementHitsRatioPerSecStats()
        {
            if (_pcHitsRatioSec != null)
            {
                lock (_pcHitsRatioSec)
                {
                    _pcHitsRatioSec.Increment();
                }
            }
        }

        public void IncrementByHitsRatioPerSecStats(long value)
        {
            if (_pcHitsRatioSec != null)
            {
                lock (_pcHitsRatioSec)
                {
                    _pcHitsRatioSec.IncrementBy(value);
                }
            }
        }

        public void IncrementHitsRatioPerSecBaseStats()
        {
            if (_pcHitsRatioSecBase != null)
            {
                lock (_pcHitsRatioSecBase)
                {
                    _pcHitsRatioSecBase.Increment();
                }
            }
        }

        public void IncrementByHitsRatioPerSecBaseStats(long value)
        {
            if (_pcHitsRatioSecBase != null)
            {
                lock (_pcHitsRatioSecBase)
                {
                    _pcHitsRatioSecBase.IncrementBy(value);
                }
            }
        }

		/// <summary> 
		/// Increment the performance counter for Cache get operations per second. 
		/// </summary>
		public void IncrementGetPerSecStats()
		{
            if (_pcGetPerSec != null) lock (_pcGetPerSec) { _pcGetPerSec.Increment(); }
		}

        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        public void IncrementByGetPerSecStats(long value)
        {
            if (_pcGetPerSec != null) lock (_pcGetPerSec) { _pcGetPerSec.IncrementBy(value); }
        }


		/// <summary> 
		/// Increment the performance counter for Cache add operations per second. 
		/// </summary>
		public void IncrementAddPerSecStats()
		{
            if (_pcAddPerSec != null) lock (_pcAddPerSec) { _pcAddPerSec.Increment(); }
		}

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        public void IncrementByAddPerSecStats(long value)
        {
            if (_pcAddPerSec != null) lock (_pcAddPerSec) { _pcAddPerSec.IncrementBy(value); }
        }

		/// <summary> 
		/// Increment the performance counter for Cache update operations per second. 
		/// </summary>
		public void IncrementUpdPerSecStats()
		{
            if (_pcUpdPerSec != null) lock (_pcUpdPerSec) { _pcUpdPerSec.Increment(); }
		}

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementByUpdPerSecStats(long value)
        {
            if (_pcUpdPerSec != null) lock (_pcUpdPerSec) { _pcUpdPerSec.IncrementBy(value); }
        }

		/// <summary> Increment the performance counter for Cache remove operations per second. </summary>
		public void IncrementDelPerSecStats()
		{
            if (_pcDelPerSec != null) lock (_pcDelPerSec) { _pcDelPerSec.Increment(); }
		}

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementByDelPerSecStats(long value)
        {
            if (_pcDelPerSec != null) lock (_pcDelPerSec) { _pcDelPerSec.IncrementBy(value); }
        }
        /// <summary> Increment the performance counter for DS failed write operations per second. </summary>
        public void IncrementDSFailedOpsPerSec()
        {
            IncrementDSFailedOpsPerSecBy(1);
        }

        public void IncrementDSFailedOpsPerSecBy(long value)
        {
            if (_pcDSFailedOpsPerSec != null) lock (_pcDSFailedOpsPerSec) { _pcDSFailedOpsPerSec.IncrementBy(value); }
        }

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementDelPerSecStats(int value)
        {
            if (_pcDelPerSec != null)
            {
                lock (_pcDelPerSec)
                {
                    _pcDelPerSec.IncrementBy(value); 
                }
            }
        }


        /// <summary> Increment the performance counter for Cache size. </summary>
        public void IncrementCacheSizeBy(long value)
        {
            if (_pcCacheSize != null)
            {
                lock (_pcCacheSize)
                {
                    _pcCacheSize.IncrementBy(value);
                }
            }
        }

        /// <summary> Increment the performance counter for Cache size. </summary>
        public void IncrementCacheSize()
        {
            if (_pcCacheSize != null)
            {
                lock (_pcCacheSize)
                {
                    IncrementCacheSizeBy(1);
                }
            }
        }

        /// <summary> Set the performance counter for Cache size. </summary>
        public void SetCacheSize(long value)
        {

            if (_pcCacheSize != null)
            {
                lock (_pcCacheSize)
                {
                    _pcCacheSize.RawValue = value;

                } 
            }
        }

        
        public void SetQueryIndexSize(long value)
        {
           
            if (_pcQueryIndexSize != null)
            {
                lock (_pcQueryIndexSize)
                {
                    _pcQueryIndexSize.RawValue = value < 0 ? 0 : value;

                }
            }
        }
        
        public void IncrementQueryIndexSizeBy(long value)
        {
            if (_pcQueryIndexSize != null)
            {
                lock (_pcQueryIndexSize)
                {
                    _pcQueryIndexSize.IncrementBy(value);
                }
            }
        }

        public void IncrementQueryIndexSize()
        {
            if (_pcQueryIndexSize != null)
            {
                lock (_pcQueryIndexSize)
                {
                    IncrementQueryIndexSizeBy(1);
                }
            }
        }

        public void SetEvictionIndexSize(long value)
        {

            if (_pcEvictionIndexSize != null)
            {
                lock (_pcEvictionIndexSize)
                {
                    _pcEvictionIndexSize.RawValue = value;

                }
            }
        }

        public void IncrementEvictionIndexSizeBy(long value)
        {
            if (_pcEvictionIndexSize != null)
            {
                lock (_pcEvictionIndexSize)
                {
                    _pcEvictionIndexSize.IncrementBy(value);
                }
            }
        }

        public void IncrementEvictionIndexSize()
        {
            if (_pcEvictionIndexSize != null)
            {
                lock (_pcEvictionIndexSize)
                {
                    IncrementEvictionIndexSizeBy(1);
                }
            }
        }

        public void SetExpirationIndexSize(long value)
        {
            if (_pcExpirationIndexSize != null)
            {
                lock (_pcExpirationIndexSize)
                {
                    _pcExpirationIndexSize.RawValue = value;
                }
            }
        }

        public void SetGroupIndexSize(long value)
        {

            if (_pcGroupIndexSize != null)
            {
                lock (_pcGroupIndexSize)
                {
                    _pcGroupIndexSize.RawValue = value;

                }
            }
        }

        public void IncrementGroupIndexSizeBy(long value)
        {
            if (_pcGroupIndexSize != null)
            {
                lock (_pcGroupIndexSize)
                {
                    _pcGroupIndexSize.IncrementBy(value);
                }
            }
        }

        public void IncrementGroupIndexSize()
        {
            if (_pcGroupIndexSize != null)
            {
                lock (_pcGroupIndexSize)
                {
                    IncrementGroupIndexSizeBy(1);
                }
            }
        }

        public void SetQueryPerSec(long value)
        {

            if (_pcQueryPerSec != null)
            {
                lock (_pcQueryPerSec)
                {
                    _pcQueryPerSec.RawValue = value;

                }
            }
        }

        public void IncrementQueryPerSecBy(long value)
        {
            if (_pcQueryPerSec != null)
            {
                lock (_pcQueryPerSec)
                {
                    _pcQueryPerSec.IncrementBy(value);
                }
            }
        }

        public void IncrementQueryPerSec()
        {
            if (_pcQueryPerSec != null)
            {
                lock (_pcQueryPerSec)
                {
                    IncrementQueryPerSecBy(1);
                }
            }
        }

        public void MsecPerQueryExecutionTimeBeginSample()
        {

            if (_pcAvgQueryExecutionTime != null) lock (_pcAvgQueryExecutionTime) { _usMsecPerQueryExecution.BeginSample(); }
           
        }

        public void MsecPerQueryExecutionTimeEndSample()
        {
            if (_pcAvgQueryExecutionTime != null)
            {
                lock (_pcAvgQueryExecutionTime)
                {
                    _usMsecPerQueryExecution.EndSample();
                    
                    _pcAvgQueryExecutionTime.IncrementBy(_usMsecPerQueryExecution.Current * 1000000); 
                    _pcAvgQueryExecutionTimeBase.Increment();
                }
            }

           
        }

       
        public void IncrementAvgQuerySize(long value)
        {
            if (_pcAvgQuerySize != null)
            {
                lock (_pcAvgQuerySize)
                {             
                    _pcAvgQuerySize.IncrementBy(value); 
                   _pcAvgQuerySizeBase.IncrementBy(1);
                   
                }
            }
        }



        /// <summary> Increment the performance counter for Cache evictions per second. </summary>
        public void IncrementEvictPerSecStats()
        {
            Interlocked.Exchange(ref this._evictions, (this._evictions + 1));
            if (_pcEvictPerSec != null) lock (_pcEvictPerSec) { _pcEvictPerSec.Increment(); }
        }
        /// <summary> Increment the performance counter for WB retry queue evictions per second. </summary>
        public void IncrementWBEvictionRate()
        {
            if (_pcWBEvicPerSec != null) lock (_pcWBEvicPerSec) { _pcWBEvicPerSec.Increment(); }
        }
        /// <summary> Increment the performance counter for Cache expirations per second. </summary>
        public void IncrementExpiryPerSecStats()
        {
            Interlocked.Exchange(ref this._expirations, (this._expirations + 1));
            if (_pcExpiryPerSec != null) lock (_pcExpiryPerSec) { _pcExpiryPerSec.Increment(); }
        }

       
        /// <summary>
        /// Increment the performance counter for read thru operation per sec
        /// </summary>
        public void IncrementReadThruPerSec()
        {
            this.IncrementReadThruPerSecBy(1);
        }

        /// <summary>
        /// Increment the performance counter for read thru operation per sec by given amount
        /// </summary>
        public void IncrementReadThruPerSecBy(long value)
        {
            if (this._pcReadThruPerSec != null) lock (this._pcReadThruPerSec) { this._pcReadThruPerSec.IncrementBy(value); }
        }

        /// <summary>
        /// Increment the performance counter for write behind operation per sec
        /// </summary>
        public void IncrementWriteBehindPerSec()
        {
            this.IncrementWriteBehindPerSecBy(1);
        }
        
        /// <summary>
        /// Increment the performance counter for write behind operation per sec by given amount
        /// </summary>
        public void IncrementWriteBehindPerSecBy(long value)
        {
            if (this._pcWriteBehindPerSec != null) lock (this._pcWriteBehindPerSec) { this._pcWriteBehindPerSec.IncrementBy(value); }
        }
        /// <summary>
        /// Increment the performance counter for write thru operation per sec
        /// </summary>
        public void IncrementWriteThruPerSec()
        {
            this.IncrementWriteThruPerSecBy(1);
        }

        /// <summary>
        /// Increment the performance counter for write thru operation per sec by given amount
        /// </summary>
        public void IncrementWriteThruPerSecBy(long value)
        {
            if (this._pcWriteThruPerSec != null) lock (this._pcWriteThruPerSec) { this._pcWriteThruPerSec.IncrementBy(value); }
        }
        /// <summary>
        /// Increment the performance counter for write behind queue operation per sec
        /// </summary>
        //public void IncrementWBQueuePerSec()
        //{
        //    this.IncrementWBQueuePerSecBy(1);
        //}
        /// <summary>
        /// Increment the performance counter for write behind queue operation per sec by given value
        /// </summary>
        public void SetWBQueueCounter(long value)
        {
            if (this._pcWBQueueCount != null) lock (this._pcWBQueueCount) { this._pcWBQueueCount.RawValue=value; }
        }
        /// <summary>
        /// Increment the performance counter for write behind queue operation per sec by given value
        /// </summary>
        public void SetWBFailureRetryCounter(long value)
        {
            if (this._pcWBFailureRertyCount != null) lock (this._pcWBFailureRertyCount) { this._pcWBFailureRertyCount.RawValue=value; }
        }
        public void decrementWBFailureRetryCounter()
        {
            if (this._pcWBFailureRertyCount != null) lock (this._pcWBFailureRertyCount) { this._pcWBFailureRertyCount.Decrement(); }
        }

        public void SetWBCurrentBatchOpsCounter(long value)
        {
            if (this._pcWBCurrenBatchOpCount != null) lock (this._pcWBCurrenBatchOpCount) { this._pcWBCurrenBatchOpCount.RawValue = value; }
        }

        /// <summary> Increments the performance counter for Cache evictions per second by given value. </summary>
        public void IncrementEvictPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._evictions, (this._evictions + value));
            if (_pcEvictPerSec != null) lock (_pcEvictPerSec) { _pcEvictPerSec.IncrementBy(value); }
        }

        /// <summary> Increment the performance counter for Cache expirations per second by given value. </summary>
        public void IncrementExpiryPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._expirations, (this._expirations + value));
            if (_pcExpiryPerSec != null) lock (_pcExpiryPerSec) { _pcExpiryPerSec.IncrementBy(value); }
        }

        /// <summary>
		/// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
		/// fetch operations. 
		/// </summary>
		public void MsecPerGetBeginSample()
		{
            if (_pcMsecPerGetAvg != null) lock (_usMsecPerGet) { _usMsecPerGet.BeginSample(); }
		}

		/// <summary>
		/// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
		/// fetch operations. 
		/// </summary>
		public void MsecPerGetEndSample()
		{
            if (_pcMsecPerGetAvg != null)
            {
                lock (_pcMsecPerGetAvg)
                {
                    _usMsecPerGet.EndSample();
                  //  TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerGetAvg.IncrementBy(_usMsecPerGet.Current * 1000000);// ts.Milliseconds);
                    _pcMsecPerGetBase.Increment();
                }
            }
		}

		/// <summary>
		/// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
		/// add operations. 
		/// </summary>
		public void MsecPerAddBeginSample()
		{
            if (_pcMsecPerAddAvg != null) lock (_pcMsecPerAddAvg) { _usMsecPerAdd.BeginSample(); }
		}

		/// <summary>
		/// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
		/// add operations. 
		/// </summary>
		public void MsecPerAddEndSample()
		{
            if (_pcMsecPerAddAvg != null)
            {
                lock (_pcMsecPerAddAvg)
                {
                    _usMsecPerAdd.EndSample();
                  //  TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerAddAvg.IncrementBy(_usMsecPerAdd.Current * 1000000 ); //ts.Milliseconds);
                    _pcMsecPerAddBase.Increment();
                    //_usMsecPerAdd.Reset();
                }
            }
		}

		/// <summary>
		/// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
		/// update operations. 
		/// </summary>
		public void MsecPerUpdBeginSample()
		{
            if (_pcMsecPerUpdAvg != null)
                lock (_pcMsecPerUpdAvg) { _usMsecPerUpd.BeginSample(); }
		}

		/// <summary>
		/// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
		/// update operations. 
		/// </summary>
		public void MsecPerUpdEndSample()
		{
            if (_pcMsecPerUpdAvg != null)
            {
                lock (_pcMsecPerUpdAvg)
                {
                    _usMsecPerUpd.EndSample();
                  //  TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerUpdAvg.IncrementBy(_usMsecPerUpd.Current * 1000000);//ts.Milliseconds);
                    _pcMsecPerUpdBase.Increment();
                    //_pcMsecPerUpdAvg.NextValue();
                }
            }
		}

		/// <summary>
		/// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
		/// remove operations. 
		/// </summary>
		public void MsecPerDelBeginSample()
		{
            if (_pcMsecPerDelAvg != null) lock (_pcMsecPerDelAvg) { _usMsecPerDel.BeginSample(); }
		}

		/// <summary>
		/// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
		/// remove operations. 
		/// </summary>
		public void MsecPerDelEndSample()
		{
            if (_pcMsecPerDelAvg != null)
            {
                lock (_pcMsecPerDelAvg)
                {
                    _usMsecPerDel.EndSample();
                 //   TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerDelAvg.IncrementBy(_usMsecPerDel.Current * 1000000); //ts.Milliseconds);
                    _pcMsecPerDelBase.Increment();
                }
            }
		}
        /// <summary>
        /// Timestamps the start of sampling interval for DataSource avg. and max. per mill-second time of 
        /// write operations. 
        /// </summary>
        public void MsecPerDSWriteBeginSample()
        {
            if (_pcMsecPerDSWriteAvg != null) lock (_pcMsecPerDSWriteAvg) { _usMsecPerDSWrite.BeginSample(); }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for DataSource avg. and max. per mill-second time of 
        /// write operations. 
        /// </summary>
        public void MsecPerDSWriteEndSample()
        {
            MsecPerDSWriteEndSample(1);
        }

        public void MsecPerDSWriteEndSample(long bulkCount)
        {
            if (_pcMsecPerDSWriteAvg != null)
            {
                lock (_pcMsecPerDSWriteAvg)
                {
                    _usMsecPerDSWrite.EndSample();
                    //   TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerDSWriteAvg.IncrementBy(_usMsecPerDSWrite.Current * 1000000); //ts.Milliseconds);
                    _pcMsecPerDSWriteBase.IncrementBy(bulkCount);
                }
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for DataSource avg. and max. per mill-second time of 
        /// write operations update in cache. 
        /// </summary>
        public void MsecPerDSUpdBeginSample()
        {
            if (_pcMsecPerDSUpdateAvg != null) lock (_pcMsecPerDSUpdateAvg) { _usMsecPerDSUp.BeginSample(); }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for DataSource avg. and max. per mill-second time of 
        /// write operations update in cache. 
        /// </summary>
        public void MsecPerDSUpdEndSample()
        {
            MsecPerDSUpdEndSample(1);
        }

        public void MsecPerDSUpdEndSample(long bulkCount)
        {
            if (_pcMsecPerDSUpdateAvg != null)
            {
                lock (_pcMsecPerDSUpdateAvg)
                {
                    _usMsecPerDSUp.EndSample();
                    //   TimeSpan ts = new TimeSpan(_usMsecPerUpd.Current);
                    _pcMsecPerDSUpdateAvg.IncrementBy(_usMsecPerDSUp.Current * 1000000); //ts.Milliseconds);
                    _pcMsecPerDSUpdateBase.IncrementBy(bulkCount);
                }
            }
        }

        /// <summary> Increment the performance counter for State Txfr's  per second. </summary>
        public void IncrementStateTxfrPerSecStats()
        {
            Interlocked.Exchange(ref this._stateXfer, (this._stateXfer + 1));
            if (_pcStateTxfrPerSec != null) lock (_pcStateTxfrPerSec) { _pcStateTxfrPerSec.Increment(); }
        }

        /// <summary> Increment the performance counter for State Txfr's per second by given value. </summary>
        public void IncrementStateTxfrPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._stateXfer, (this._stateXfer + value));
            if (_pcStateTxfrPerSec != null) lock (_pcStateTxfrPerSec) { _pcStateTxfrPerSec.IncrementBy(value); }
        }


        /// <summary> Increment the performance counter for DS update in cache per second. </summary>
        public void IncrementDSUpdatePerSec()
        {
            if (_pcDSUpdatesPerSec != null) lock (_pcDSUpdatesPerSec) { _pcDSUpdatesPerSec.Increment(); }
        }

        /// <summary> Increment the performance counter for DS update in cache per second by given value. </summary>
        public void IncrementDSUpdatePerSecBy(long value)
        {
            if (_pcDSUpdatesPerSec != null) lock (_pcDSUpdatesPerSec) { _pcDSUpdatesPerSec.IncrementBy(value); }
        }
       

        /// <summary>
        /// Count of items that are expired till now
        /// </summary>
        public float Expirations
        {
            get { return Interlocked.Exchange(ref this._expirations, 0); }
        }

        /// <summary>
        /// Count of items that are evicted till now
        /// </summary>
        public float Eviction
        {
            get { return Interlocked.Exchange(ref this._evictions, 0); }
        }

        /// <summary>
        /// Count of items that are state transfered till now
        /// </summary>
        public float StateXfer
        {
            get { return Interlocked.Exchange(ref this._stateXfer, 0); }
        }

        /// <summary>
        /// Count of items that are balanced till now
        /// </summary>
        public float DataBalance
        {
            get { return Interlocked.Exchange(ref this._dataBalance, 0); }
        }
        /// <summary> 
        /// Increment the performance counter for Mirror Queue size by one. 
        /// </summary>
        public void IncrementMirrorQueueSizeStats(long count)
        {
            if (_pcMirrorQueueSize != null)
            {
                lock (_pcMirrorQueueSize)
                {
                    _pcMirrorQueueSize.RawValue = count;
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for SlidingIndex Queue size by one. 
        /// </summary>
        public void IncrementSlidingIndexQueueSizeStats(long count)
        {
            if (_pcSlidingIndexQueueSize != null)
            {
                lock (_pcSlidingIndexQueueSize)
                {
                    _pcSlidingIndexQueueSize.RawValue = count;
                }
            }
        }

        #region StatisticCounter Members


        public ILogger NCacheLog
        {
            get
            {
                return _ncacheLog;
            }
            set
            {
                _ncacheLog = value;
            }
        }

        #endregion




        public void WaitingTasksCount(long value)
        {
            if (_mrWaitingTasks != null)
            {
                lock (_mrWaitingTasks)
                {
                    _mrWaitingTasks.RawValue = value;
                }
            }
        }

        public void IncrementWaitingTasks()
        {
            if (_mrWaitingTasks != null)
            {
                lock (_mrWaitingTasks)
                {
                    _mrWaitingTasks.Increment();
                }
            }
        }

        public void DecrementWaitingTasks()
        {
            if (_mrWaitingTasks != null)
            {
                lock (_mrWaitingTasks)
                {
                    if(_mrWaitingTasks.RawValue != 0)
                        _mrWaitingTasks.Decrement();
                }
            }
        }
        
        public void IncrementWaitingTasksBy(long value)
        {
            if (_mrWaitingTasks != null)
            {
                lock (_mrWaitingTasks)
                {
                    _mrWaitingTasks.IncrementBy(value);
                }
            }
        }

        public void RunningTasksCount(long value)
        {
            if (_mrRunningTasks != null)
            {
                lock (_mrRunningTasks)
                {
                    _mrRunningTasks.RawValue = value;
                }
            }
        }

        public void IncrementRunningTasks()
        {
            if (_mrRunningTasks != null)
            {
                lock (_mrRunningTasks)
                {
                    _mrRunningTasks.Increment();
                }
            }
        }

        public void DecrementRunningTasks()
        {
            if (_mrRunningTasks != null)
            {
                lock (_mrRunningTasks)
                {
                    if(_mrRunningTasks.RawValue != 0)
                        _mrRunningTasks.Decrement();
                }
            }
        }
        
        public void IncrementRunningTasksBy(long value)
        {
            if (_mrRunningTasks != null)
            {
                lock (_mrRunningTasks)
                {
                    _mrRunningTasks.IncrementBy(value);
                }
            }
        }

        public void MappedPerSec(long value)
        {
            if (_mrMapperPerSec != null)
            {
                lock (_mrMapperPerSec)
                {
                    _mrMapperPerSec.RawValue = value;
                }
            }
        }

        public void IncrementMappedPerSecRate()
        {
            if (_mrMapperPerSec != null)
            {
                lock (_mrMapperPerSec)
                {
                    _mrMapperPerSec.Increment();
                }
            }
        }

        public void CombinedPerSec(long value)
        {
            if (_mrCombinedPerSec != null)
            {
                lock (_mrCombinedPerSec)
                {
                    _mrCombinedPerSec.RawValue = value;
                }
            }
        }

        public void IncrementCombinedPerSecRate()
        {
            if (_mrCombinedPerSec != null)
            {
                lock (_mrCombinedPerSec)
                {
                    _mrCombinedPerSec.Increment();
                }
            }
        }

        public void ReducedPerSec(long value)
        {
            if (_mrReducedPerSec != null)
            {
                lock (_mrReducedPerSec)
                {
                    _mrReducedPerSec.RawValue = value;
                }
            }
        }

        public void IncrementReducedPerSecRate()
        {
            if (_mrReducedPerSec != null)
            {
                lock (_mrReducedPerSec)
                {
                    _mrReducedPerSec.Increment();
                }
            }
        }

        public void IncrementRunningReaders()
        {
            if (_runningReader != null)
            {
                lock (_runningReader)
                {
                    _runningReader.Increment();
                }
            }
        }

        public void DecrementRunningReaders()
        {
            if (_runningReader != null)
            {
                lock (_runningReader)
                {
                    if (_runningReader.RawValue != 0)
                        _runningReader.Decrement();
                }
            }
        }
        #region PUB_SUB

        public void IncrementMsecPerMessagePublish(long count)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.AvgPublishMessage))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.AvgPublishMessage];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(count * 1000000);
                        _pubsubCounterList[CounterNames.AvgPublishMessageBase].Increment();
                    }
                }
            }
        }

        public void SetPubSubCounter(long value, string counterType)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(counterType))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[counterType];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.RawValue = value;
                    }
                }
            }
        }

        public void PubSubCounterSetter(string counterType, PerformanceOperator counterOperation)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(counterType))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[counterType];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        switch (counterOperation)
                        {
                            case PerformanceOperator.Increment:
                                performanceCounter.Increment();
                                break;
                            case PerformanceOperator.Decrement:
                                if (performanceCounter.RawValue != 0)
                                    performanceCounter.Decrement();
                                break;
                        }
                    }
                }
            }
        }


        public void IncrementMessagePublishedPerSec()
        {
            PubSubCounterSetter(CounterNames.MessagePublishPerSec, PerformanceOperator.Increment);
        }

        public void IncrementMessageDeliverPerSec(long value)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageDeliveryPerSec))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.MessageDeliveryPerSec];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value);
                    }
                }
            }
        }

        public void IncrementMessageDeliverPerSec()
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageDeliveryPerSec))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.MessageDeliveryPerSec];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.Increment();
                    }
                }
            }
        }

        public void IncrementMessageExpiredPerSec(long value)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageExpiredPerSec))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.MessageExpiredPerSec];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value);
                    }
                }
            }
        }

        #endregion

    }

}
