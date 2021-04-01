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
using System.Diagnostics;
using Alachisoft.NCache.Common.Stats;
using System.Threading;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Common.Logger;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common.Collections;
using Alachisoft.NCache.Common.Caching;

namespace Alachisoft.NCache.Caching.Statistics
{
    /// <summary>
    /// Summary description for PerfStatsCollector.
    /// </summary>
    public class PerfStatsCollector : IDisposable, StatisticCounter
    {
        HPTime initTime;
        /// <summary> Instance name. </summary>
        private string _instanceName;
        /// <summary> Port number. </summary>
        private string _port;
        /// <summary>
        /// whether extended cache monitoring is enabled or not.
        /// </summary>
        private bool _enableExtendedMonitoring;

        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcCount = null;
        /// <summary> performance counter for Cache item count. </summary>
        private PerformanceCounter _pcCachelastAccessCount = null;
        /// <summary> performance counter for Cache hits per second. </summary>
        private PerformanceCounter _pcHitsPerSec = null;
        /// <summary> performance counter for Cache misses per second. </summary>
        private PerformanceCounter _pcMissPerSec = null;
        private PerformanceCounter _pcHitsRatioSec = null;
        private PerformanceCounter _pcHitsRatioSecBase = null;
        /// <summary> performance counter for Cache get operations per second. </summary>
        private PerformanceCounter _pcGetPerSec = null;
        /// <summary> performance counter for Cache add operations per second. </summary>
        private PerformanceCounter _pcAddPerSec = null;
        /// <summary> performance counter for Cache update operations per second. </summary>
        private PerformanceCounter _pcUpdPerSec = null;
        /// <summary> performance counter for Cache remove operations per second. </summary>
        private PerformanceCounter _pcDelPerSec = null;
        /// <summary> performance counter for Cache evictions per second. </summary>
        private PerformanceCounter _pcEvictPerSec = null;
        /// <summary> performance counter for Cache expirations per second. </summary>
        private PerformanceCounter _pcExpiryPerSec = null;
        /// <summary> performance counter for State Txfr's per second. </summary>
        private PerformanceCounter _pcStateTxfrPerSec = null;
        /// <summary> performance counter for Data Balance per second for Partitioned and Partioned Replica. </summary>
        private PerformanceCounter _pcDataBalPerSec = null;
        /// <summary> performance counter for Mirror Queue size. </summary>
        private PerformanceCounter _pcMirrorQueueSize = null;
        private PerformanceCounter _pcSlidingIndexQueueSize = null;

        /// <summary> performance counter for Cache avg. per milli-second time of get operations. </summary>
        private PerformanceCounter _pcMsecPerGetAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of add operations. </summary>
        private PerformanceCounter _pcMsecPerAddAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of update operations. </summary>
        private PerformanceCounter _pcMsecPerUpdAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of remove operations. </summary>
        private PerformanceCounter _pcMsecPerDelAvg = null;
        /// <summary> performance counter for Cache max. per milli-second time of get operations. </summary>
        private PerformanceCounter _pcMsecPerGetBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of add operations. </summary>
        private PerformanceCounter _pcMsecPerAddBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of update operations. </summary>
        private PerformanceCounter _pcMsecPerUpdBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of remove operations. </summary>
        private PerformanceCounter _pcMsecPerDelBase = null;

        /// <summary> usage statistics for Cache per milli-second time of get operations. </summary>
        private UsageStats _usMsecPerGet = null;
        /// <summary> usage statistics for Cache per milli-second time of add operations. </summary>
        private UsageStats _usMsecPerAdd = null;
        /// <summary> usage statistics for Cache per milli-second time of update operations. </summary>
        private UsageStats _usMsecPerUpd = null;
        /// <summary> usage statistics for Cache per milli-second time of remove operations. </summary>
        private UsageStats _usMsecPerDel = null;

        /// <summary>performance counter for Size of the cache including store and all indices.. </summary>
        private PerformanceCounter _pcCacheSize = null;
        /// <summary> performance counter for Size of expiration indices define on cache. </summary>
        private PerformanceCounter _pcExpirationIndexSize = null;
        /// <summary> performance counter for Size of eviction indices define on cache. </summary>
        private PerformanceCounter _pcEvictionIndexSize = null;
        /// <summary> performance counter for Size of group and sub group indices.. </summary>

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

        private ILogger _ncacheLog;

		/// <summary> Category name of counter performance data.</summary>
        /// 
        private const string			PC_CATEGORY = "NCache";

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

                    if (!PerformanceCounterCategory.Exists(PC_CATEGORY, "."))
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    if (NCacheLog.IsInfoEnabled) NCacheLog.Info("PerfStatsCollector.UserHasAccessRights", e.Message);
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
            lock (this)
            {
                if (_pcCount != null)
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
                if (_pcHitsPerSec != null)
                {
                    _pcHitsPerSec.RemoveInstance();
                    _pcHitsPerSec.Dispose();
                    _pcHitsPerSec = null;
                }
                if (_pcMissPerSec != null)
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
                if (_pcAddPerSec != null)
                {
                    _pcAddPerSec.RemoveInstance();
                    _pcAddPerSec.Dispose();
                    _pcAddPerSec = null;
                }
                if (_pcGetPerSec != null)
                {
                    _pcGetPerSec.RemoveInstance();
                    _pcGetPerSec.Dispose();
                    _pcGetPerSec = null;
                }
                if (_pcUpdPerSec != null)
                {
                    _pcUpdPerSec.RemoveInstance();
                    _pcUpdPerSec.Dispose();
                    _pcUpdPerSec = null;
                }
                if (_pcDelPerSec != null)
                {
                    _pcDelPerSec.RemoveInstance();
                    _pcDelPerSec.Dispose();
                    _pcDelPerSec = null;
                }
                if (_pcMsecPerAddAvg != null)
                {
                    _pcMsecPerAddAvg.RemoveInstance();
                    _pcMsecPerAddAvg.Dispose();
                    _pcMsecPerAddAvg = null;
                }
                if (_pcMsecPerDelAvg != null)
                {
                    _pcMsecPerDelAvg.RemoveInstance();
                    _pcMsecPerDelAvg.Dispose();
                    _pcMsecPerDelAvg = null;
                }
                if (_pcMsecPerGetAvg != null)
                {
                    _pcMsecPerGetAvg.RemoveInstance();
                    _pcMsecPerGetAvg.Dispose();
                    _pcMsecPerGetAvg = null;
                }
                if (_pcMsecPerUpdAvg != null)
                {
                    _pcMsecPerUpdAvg.RemoveInstance();
                    _pcMsecPerUpdAvg.Dispose();
                    _pcMsecPerUpdAvg = null;
                }
                if (_pcMsecPerAddBase != null)
                {
                    _pcMsecPerAddBase.RemoveInstance();
                    _pcMsecPerAddBase.Dispose();
                    _pcMsecPerAddBase = null;
                }
                if (_pcMsecPerDelBase != null)
                {
                    _pcMsecPerDelBase.RemoveInstance();
                    _pcMsecPerDelBase.Dispose();
                    _pcMsecPerDelBase = null;
                }
                if (_pcMsecPerGetBase != null)
                {
                    _pcMsecPerGetBase.RemoveInstance();
                    _pcMsecPerGetBase.Dispose();
                    _pcMsecPerGetBase = null;
                }
                if (_pcMsecPerUpdBase != null)
                {
                    _pcMsecPerUpdBase.RemoveInstance();
                    _pcMsecPerUpdBase.Dispose();
                    _pcMsecPerUpdBase = null;
                }
                if (_pcEvictPerSec != null)
                {
                    _pcEvictPerSec.RemoveInstance();
                    _pcEvictPerSec.Dispose();
                    _pcEvictPerSec = null;
                }
                if (_pcExpiryPerSec != null)
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
                if (_pcDataBalPerSec != null)
                {
                    _pcDataBalPerSec.RemoveInstance();
                    _pcDataBalPerSec.Dispose();
                    _pcDataBalPerSec = null;
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
                if (_pcMessageCount != null)
                {
                    _pcMessageCount.RemoveInstance();
                    _pcMessageCount.Dispose();
                    _pcMessageCount = null;
                }
                if (_pcMessageDeliverPerSec != null)
                {
                    _pcMessageDeliverPerSec.RemoveInstance();
                    _pcMessageDeliverPerSec.Dispose();
                    _pcMessageDeliverPerSec = null;
                }
                if (_pcMessageExpiredPerSec != null)
                {
                    _pcMessageExpiredPerSec.RemoveInstance();
                    _pcMessageExpiredPerSec.Dispose();
                    _pcMessageExpiredPerSec = null;
                }
                if (_pcMessagePublishPerSec != null)
                {
                    _pcMessagePublishPerSec.RemoveInstance();
                    _pcMessagePublishPerSec.Dispose();
                    _pcMessagePublishPerSec = null;
                }
                if (_pcMessageStoreSize != null)
                {
                    _pcMessageStoreSize.RemoveInstance();
                    _pcMessageStoreSize.Dispose();
                    _pcMessageStoreSize = null;
                }
                if (_pcTopicCount != null)
                {
                    _pcTopicCount.RemoveInstance();
                    _pcTopicCount.Dispose();
                    _pcTopicCount = null;
                }

               
            }
        }

        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        public void InitializePerfCounters(bool inproc, bool ModuleConfigured = false)
        {
#if NETCORE
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
                return;
#endif
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

                    _pcCount = new PerformanceCounter(PC_CATEGORY, CounterNames.Count, _instanceName, false);
                    _pcCachelastAccessCount = new PerformanceCounter(PC_CATEGORY, CounterNames.CacheLastAccessCount, _instanceName, false);
                    _pcHitsPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.HitsPerSec, _instanceName, false);
                    _pcMissPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MissPerSec, _instanceName, false);
                    _pcHitsRatioSec = new PerformanceCounter(PC_CATEGORY, CounterNames.HitsRatioSec, _instanceName, false);
                    _pcHitsRatioSecBase = new PerformanceCounter(PC_CATEGORY, CounterNames.HitsRatioSecBase, _instanceName, false);
                    _pcAddPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.AddPerSec, _instanceName, false);
                    _pcGetPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.GetPerSec, _instanceName, false);
                    _pcUpdPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.UpdPerSec, _instanceName, false);
                    _pcDelPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.DelPerSec, _instanceName, false);
                    _pcEvictPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.EvictPerSec, _instanceName, false);
                    _pcExpiryPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.ExpiryPerSec, _instanceName, false);
                    _pcStateTxfrPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.StateTxfrPerSec, _instanceName, false);
                    _pcDataBalPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.DataBalPerSec, _instanceName, false);

                    _pcMsecPerGetAvg = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerGetAvg, _instanceName, false);
                    _pcMsecPerGetBase = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerGetBase, _instanceName, false);
                    _usMsecPerGet = new UsageStats();
                    _pcMsecPerAddAvg = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerAddAvg, _instanceName, false);
                    _pcMsecPerAddBase = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerAddBase, _instanceName, false);
                    _usMsecPerAdd = new UsageStats();
                    _pcMsecPerUpdAvg = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerUpdAvg, _instanceName, false);
                    _pcMsecPerUpdBase = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerUpdBase, _instanceName, false);
                    _usMsecPerUpd = new UsageStats();
                    _pcMsecPerDelAvg = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerDelAvg, _instanceName, false);
                    _pcMsecPerDelBase = new PerformanceCounter(PC_CATEGORY, CounterNames.MsecPerDelBase, _instanceName, false);
                    _usMsecPerDel = new UsageStats();

                    _pcMirrorQueueSize = new PerformanceCounter(PC_CATEGORY, CounterNames.MirrorQueueSize, _instanceName, false);
                    _pcSlidingIndexQueueSize = new PerformanceCounter(PC_CATEGORY, CounterNames.SlidingIndexQueueSize, _instanceName, false);

                    _pcCacheSize = new PerformanceCounter(PC_CATEGORY, CounterNames.CacheSize, _instanceName, false);
                    _pcCacheSize.RawValue = 0;
                    _pcEvictionIndexSize = new PerformanceCounter(PC_CATEGORY, CounterNames.EvictionIndexSize, _instanceName, false);
                    _pcExpirationIndexSize = new PerformanceCounter(PC_CATEGORY, CounterNames.ExpirationIndexSize, _instanceName, false);
                    

                    #region Pub_Sub
                    _pubsubCounterList = new Dictionary<string, PerformanceCounter>();
                    _pcMessageCount = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageCount, _instanceName, false);
                    _pcTopicCount = new PerformanceCounter(PC_CATEGORY, CounterNames.TopicCount, _instanceName, false);
                    _pcMessageStoreSize = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageStoreSize, _instanceName, false);
                    _pcMessagePublishPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MessagePublishPerSec, _instanceName, false);
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
                NCache.Common.AppUtil.LogEvent(e.ToString(), EventLogEntryType.Error);
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
                _pcCount.IncrementBy(count);
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache hits per second. 
        /// </summary>
        public void IncrementHitsPerSecStats()
        {
            if (_pcHitsPerSec != null) _pcHitsPerSec.Increment();
        }

        /// <summary> 
        /// Increment the performance counter for Cache hits per second. 
        /// </summary>
        public void IncrementByHitsPerSecStats(long value)
        {
            if (_pcHitsPerSec != null) _pcHitsPerSec.IncrementBy(value);
        }

        /// <summary>
        /// Increment the performance counter for Cache misses per second. 
        /// </summary>
        public void IncrementMissPerSecStats()
        {
            if (_pcMissPerSec != null) _pcMissPerSec.Increment();
        }

        /// <summary>
        /// Increment the performance counter for Cache misses per second. 
        /// </summary>
        public void IncrementByMissPerSecStats(long value)
        {
            if (_pcMissPerSec != null) _pcMissPerSec.IncrementBy(value);
        }

        public void IncrementHitsRatioPerSecStats()
        {
            if (_pcHitsRatioSec != null) _pcHitsRatioSec.Increment();
        }

        public void IncrementByHitsRatioPerSecStats(long value)
        {
            if (_pcHitsRatioSec != null) _pcHitsRatioSec.IncrementBy(value);
        }

        public void IncrementHitsRatioPerSecBaseStats()
        {
            if (_pcHitsRatioSecBase != null) _pcHitsRatioSecBase.Increment();
        }

        public void IncrementByHitsRatioPerSecBaseStats(long value)
        {
            if (_pcHitsRatioSecBase != null) _pcHitsRatioSecBase.IncrementBy(value);
        }

        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        public void IncrementGetPerSecStats()
        {
            if (_pcHitsRatioSecBase != null) _pcGetPerSec.Increment();
        }

        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        public void IncrementByGetPerSecStats(long value)
        {
            if (_pcHitsRatioSecBase != null) _pcGetPerSec.IncrementBy(value);
        }


        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        public void IncrementAddPerSecStats()
        {
            if (_pcAddPerSec != null) _pcAddPerSec.Increment();
        }

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        public void IncrementByAddPerSecStats(long value)
        {
            if (_pcAddPerSec != null) _pcAddPerSec.IncrementBy(value);
        }

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementUpdPerSecStats()
        {
            if (_pcUpdPerSec != null) _pcUpdPerSec.Increment();
        }

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementByUpdPerSecStats(long value)
        {
            if (_pcUpdPerSec != null) _pcUpdPerSec.IncrementBy(value);
        }

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementDelPerSecStats()
        {
            if (_pcDelPerSec != null) _pcDelPerSec.Increment();
        }

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementByDelPerSecStats(long value)
        {
            if (_pcDelPerSec != null) _pcDelPerSec.IncrementBy(value);
        }
        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementDelPerSecStats(int value)
        {
            if (_pcDelPerSec != null) _pcDelPerSec.IncrementBy(value);
        }

        /// <summary> Increment the performance counter for Cache size. </summary>
        public void IncrementCacheSizeBy(long value)
        {
            if (_pcCacheSize != null) _pcCacheSize.IncrementBy(value);
        }

        /// <summary> Increment the performance counter for Cache size. </summary>
        public void IncrementCacheSize()
        {
            if (_pcCacheSize != null) _pcCacheSize.Increment();
        }

        /// <summary> Set the performance counter for Cache size. </summary>
        public void SetCacheSize(long value)
        {
            if (_pcCacheSize != null) lock (_pcCacheSize) _pcCacheSize.RawValue = value;
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
            if (_pcEvictionIndexSize != null) _pcEvictionIndexSize.IncrementBy(value);
        }

        public void IncrementEvictionIndexSize()
        {
            if (_pcEvictionIndexSize != null) _pcEvictionIndexSize.Increment();
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
        /// <summary> Increment the performance counter for Cache evictions per second. </summary>
        public void IncrementEvictPerSecStats()
        {
            if (_pcEvictPerSec != null) _pcEvictPerSec.Increment();
        }
        /// <summary> Increment the performance counter for Cache expirations per second. </summary>
        public void IncrementExpiryPerSecStats()
        {
            if (_pcExpiryPerSec != null) _pcExpiryPerSec.Increment();
        }

        /// <summary> Increments the performance counter for Cache evictions per second by given value. </summary>
        public void IncrementEvictPerSecStatsBy(long value)
        {
            if (_pcEvictPerSec != null) _pcEvictPerSec.IncrementBy(value);
        }

        /// <summary> Increment the performance counter for Cache expirations per second by given value. </summary>
        public void IncrementExpiryPerSecStatsBy(long value)
        {
            if (_pcExpiryPerSec != null) _pcExpiryPerSec.IncrementBy(value);
        }

        /// <summary>
		/// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
		/// fetch operations. 
		/// </summary>
		public void MsecPerGetBeginSample()
        {
            if (_usMsecPerGet != null) _usMsecPerGet.BeginSample();
        }

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// fetch operations. 
        /// </summary>
        public void MsecPerGetEndSample()
        {
            if (_usMsecPerGet != null)
            {
                _usMsecPerGet.EndSample();
                _pcMsecPerGetAvg.IncrementBy(_usMsecPerGet.Current * 1000000);// ts.Milliseconds);
                _pcMsecPerGetBase.Increment();
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// add operations. 
        /// </summary>
        public void MsecPerAddBeginSample()
        {
            if (_usMsecPerAdd != null) _usMsecPerAdd.BeginSample();
        }

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// add operations. 
        /// </summary>
        public void MsecPerAddEndSample()
        {
            if (_usMsecPerAdd != null)
            {
                _usMsecPerAdd.EndSample();
                _pcMsecPerAddAvg.IncrementBy(_usMsecPerAdd.Current * 1000000); //ts.Milliseconds);
                _pcMsecPerAddBase.Increment();
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// update operations. 
        /// </summary>
        public void MsecPerUpdBeginSample()
        {
            if (_pcMsecPerUpdAvg != null) _usMsecPerUpd.BeginSample();
        }

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// update operations. 
        /// </summary>
        public void MsecPerUpdEndSample()
        {
            if (_pcMsecPerUpdAvg != null)
            {
                _usMsecPerUpd.EndSample();
                _pcMsecPerUpdAvg.IncrementBy(_usMsecPerUpd.Current * 1000000);//ts.Milliseconds);
                _pcMsecPerUpdBase.Increment();
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// remove operations. 
        /// </summary>
        public void MsecPerDelBeginSample()
        {
            if (_pcMsecPerDelAvg != null) _usMsecPerDel.BeginSample();
        }

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// remove operations. 
        /// </summary>
        public void MsecPerDelEndSample()
        {
            if (_pcMsecPerDelAvg != null)
            {
                _usMsecPerDel.EndSample();
                _pcMsecPerDelAvg.IncrementBy(_usMsecPerDel.Current * 1000000); //ts.Milliseconds);
                _pcMsecPerDelBase.Increment();
            }
        }

        /// <summary>Resets the State Transfer perfmon counter value</summary>
        public void ResetStateTransferPerfmonCounter()
        {
            if (_pcStateTxfrPerSec != null) lock (_pcStateTxfrPerSec) { _pcStateTxfrPerSec.RawValue = 0; } 
        }

        /// <summary> Increment the performance counter for State Txfr's  per second. </summary>
        public void IncrementStateTxfrPerSecStats()
        {
            if (_pcStateTxfrPerSec != null) _pcStateTxfrPerSec.Increment();
        }

        /// <summary> Increment the performance counter for State Txfr's per second by given value. </summary>
        public void IncrementStateTxfrPerSecStatsBy(long value)
        {
            if (_pcStateTxfrPerSec != null) _pcStateTxfrPerSec.IncrementBy(value);
        }


        /// <summary> Increment the performance counter for Data Balance per second. </summary>
        public void IncrementDataBalPerSecStats()
        {
            if (_pcDataBalPerSec != null) _pcDataBalPerSec.Increment();
        }

        /// <summary> Increment the performance counter for Data Balance per second by given value. </summary>
        public void IncrementDataBalPerSecStatsBy(long value)
        {
            if (_pcDataBalPerSec != null) _pcDataBalPerSec.IncrementBy(value);
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
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        #endregion




        #region PUB_SUB

        public void IncrementMsecPerMessagePublish(long count)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.AvgPublishMessage))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.AvgPublishMessage];
                if (performanceCounter != null)
                {
                    performanceCounter.IncrementBy(count * 1000000);
                    _pubsubCounterList[CounterNames.AvgPublishMessageBase].Increment();
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
                    switch (counterOperation)
                    {
                        case PerformanceOperator.Increment:
                            performanceCounter.Increment();
                            break;
                        case PerformanceOperator.Decrement:
                            performanceCounter.Decrement();
                            break;
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
                    performanceCounter.IncrementBy(value);
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
                    performanceCounter.Increment();
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
                    performanceCounter.IncrementBy(value);
                }
            }
        }

        public double GetCounterValue(string counterName)
        {
            double value = 0.0;
            PerformanceCounter counter = null;

            switch (counterName)
            {
                case CounterNames.MirrorQueueSize:
                    counter = _pcMirrorQueueSize;
                    break;
                case CounterNames.SlidingIndexQueueSize:
                    counter = _pcSlidingIndexQueueSize;
                    break;
                case CounterNames.EvictionIndexSize:
                    counter = _pcEvictionIndexSize;
                    break;
                case CounterNames.ExpirationIndexSize:
                    counter = _pcExpirationIndexSize;
                    break;
            }

            if (counter != null)
            {
                lock (counter)
                {
                    value = counter.NextValue();
                }
            }

            return value;
        }
        
        #endregion

    }
}
