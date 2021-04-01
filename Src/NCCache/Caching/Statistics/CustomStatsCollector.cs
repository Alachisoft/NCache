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
using Alachisoft.NCache.Caching.Statistics;
using System;
using System.Collections.Generic;
using System.Text;
using Alachisoft.NCache.Common.Logger;
using Alachisoft.NCache.Common.Util;
using System.Threading;
using System.Diagnostics;
using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Caching;
using Alachisoft.NCache.Common.Collections;

namespace Alachisoft.NCache.Caching.Statistics
{
    public abstract class CounterBase
    {

    }

    public class CustomStatsCollector : IDisposable, StatisticCounter
    {
        private string _instanceName;

        private float _expirations = 0;
        private float _evictions = 0;
        private float _stateXfer = 0;
        private float _dataBalance = 0;

        private ILogger _ncacheLog;

        private PerformanceCounterBase _countStats;
        private PerformanceCounterBase _sizeStats;
        private PerformanceCounterBase _addStats;
        private PerformanceCounterBase _updateStats;
        private PerformanceCounterBase _fetchStats;
        private PerformanceCounterBase _hitStats;
        private PerformanceCounterBase _missStats;
        private PerformanceCounterBase _cacheLastAccessCountStats;
        private PerformanceCounterBase _hitsRatioSecStats;
        private PerformanceCounterBase _deleteStats;
        private PerformanceCounterBase _evictPerSecStats;
        private PerformanceCounterBase _expiryPerSecStats;
        private PerformanceCounterBase _stateTxfrPerSecStats;
        private PerformanceCounterBase _dataBalPerSecStats;
        private PerformanceCounterBase _mirrorQueueSizeStats;
        private PerformanceCounterBase _slidingIndexQueueSizeStats;
        private PerformanceCounterBase _msecPerGetAvgStats;
        private PerformanceCounterBase _msecPerAddAvgStats;
        private PerformanceCounterBase _msecPerUpdateAvgStats;
        private PerformanceCounterBase _msecPerDeleteAvgStats;
        private UsageStats _usageMsecPerGetStats;
        private UsageStats _usageMsecPerAddStats;
        private UsageStats _usageMsecPerUpdateStats;
        private UsageStats _usageMsecPerDeleteStats;
        private PerformanceCounterBase _expirationIndexSizeStats;
        private PerformanceCounterBase _evictionIndexSizeStats;
        private PerformanceCounterBase _messageCountStats;
        private PerformanceCounterBase _topicCountStats;
        private PerformanceCounterBase _messageStoreSizeStats;
        private PerformanceCounterBase _messagePublishPerSecStats;
        private PerformanceCounterBase _messageDeliverPerSecStats;
        private PerformanceCounterBase _messageExpiredPerSecStats;

    
        private Dictionary<string, PerformanceCounterBase> _pubsubCounterList;

        public CustomStatsCollector(string instanceName, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, 0, inProc);

        }

        public CustomStatsCollector(string instanceName, int port, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, port, inProc);
        }


        public string InstanceName { get { return _instanceName; } set { _instanceName = value; } }

        public bool UserHasAccessRights
        {
            get { return true; }
        }

        public void InitializePerfCounters(bool inproc, bool ModuleConfigured = false)
        {
            try
            {
                _countStats = new NumberOfItemCounter(CounterNames.Count, _instanceName);
                _sizeStats = new NumberOfItemCounter(CounterNames.CacheSize, _instanceName);

                _addStats = new RateOfCounter(CounterNames.AddPerSec, _instanceName);
                _updateStats = new RateOfCounter(CounterNames.UpdPerSec, _instanceName);
                _fetchStats = new RateOfCounter(CounterNames.GetPerSec, _instanceName);
                _hitStats = new RateOfCounter(CounterNames.HitsPerSec, _instanceName);
                _missStats = new RateOfCounter(CounterNames.MissPerSec, _instanceName);
                _usageMsecPerGetStats = new UsageStats();
                _cacheLastAccessCountStats = new NumberOfItemCounter(CounterNames.CacheLastAccessCount, _instanceName);
                _hitsRatioSecStats = new RateOfCounter(CounterNames.HitsRatioSec, _instanceName);
                _deleteStats = new RateOfCounter(CounterNames.DelPerSec, _instanceName);
                _evictPerSecStats = new RateOfCounter(CounterNames.EvictPerSec, _instanceName);
                _expiryPerSecStats = new RateOfCounter(CounterNames.ExpiryPerSec, _instanceName);
                _stateTxfrPerSecStats = new RateOfCounter(CounterNames.StateTxfrPerSec, _instanceName);
                _dataBalPerSecStats = new RateOfCounter(CounterNames.DataBalPerSec, _instanceName);
                _msecPerGetAvgStats = new AverageCounter(CounterNames.MsecPerGetAvg, _instanceName);
                _msecPerAddAvgStats = new AverageCounter(CounterNames.MsecPerAddAvg, _instanceName);
                _usageMsecPerAddStats = new UsageStats();
                _msecPerUpdateAvgStats = new AverageCounter(CounterNames.MsecPerUpdAvg, _instanceName);
                _usageMsecPerUpdateStats = new UsageStats();
                _msecPerDeleteAvgStats = new AverageCounter(CounterNames.MsecPerDelAvg, _instanceName);
                _usageMsecPerDeleteStats = new UsageStats();
                _mirrorQueueSizeStats = new NumberOfItemCounter(CounterNames.MirrorQueueSize, _instanceName);
                _slidingIndexQueueSizeStats = new NumberOfItemCounter(CounterNames.SlidingIndexQueueSize, _instanceName);
                _evictionIndexSizeStats = new NumberOfItemCounter(CounterNames.EvictionIndexSize, _instanceName);
                _expirationIndexSizeStats = new NumberOfItemCounter(CounterNames.ExpirationIndexSize, _instanceName);

                #region Pub_Sub
                _pubsubCounterList = new Dictionary<string, PerformanceCounterBase>();
                _messageCountStats = new NumberOfItemCounter(CounterNames.MessageCount, _instanceName);
                _topicCountStats = new NumberOfItemCounter(CounterNames.TopicCount, _instanceName);
                _messageStoreSizeStats = new NumberOfItemCounter(CounterNames.MessageStoreSize, _instanceName);
                _messagePublishPerSecStats = new RateOfCounter(CounterNames.MessagePublishPerSec, _instanceName);
                _messageDeliverPerSecStats = new RateOfCounter(CounterNames.MessageDeliveryPerSec, _instanceName);
                _messageExpiredPerSecStats = new RateOfCounter(CounterNames.MessageExpiredPerSec, _instanceName);
                _pubsubCounterList.Add(CounterNames.MessageCount, _messageCountStats);
                _pubsubCounterList.Add(CounterNames.TopicCount, _topicCountStats);
                _pubsubCounterList.Add(CounterNames.MessageStoreSize, _messageStoreSizeStats);
                _pubsubCounterList.Add(CounterNames.MessagePublishPerSec, _messagePublishPerSecStats);
                _pubsubCounterList.Add(CounterNames.MessageDeliveryPerSec, _messageDeliverPerSecStats);
                _pubsubCounterList.Add(CounterNames.MessageExpiredPerSec, _messageExpiredPerSecStats);
            }
            catch (Exception ex)
            {
                throw new Exception("PerfStatsCollector.PerfStatsCollector()", ex);
            }

            #endregion

         
        }

        public ILogger NCacheLog
        {
            get { return _ncacheLog; }
            set { _ncacheLog = value; }
        }

        public void Dispose()
        {
            if (_countStats != null) _countStats.Reset();
            if (_sizeStats != null) _sizeStats.Reset();
            if (_addStats != null) _addStats.Reset();
            if (_updateStats != null) _updateStats.Reset();
            if (_fetchStats != null) _fetchStats.Reset();
            if (_hitStats != null) _hitStats.Reset();
            if (_missStats != null) _missStats.Reset();
            if (_cacheLastAccessCountStats != null) _cacheLastAccessCountStats.Reset();
            if (_hitsRatioSecStats != null) _hitsRatioSecStats.Reset();
            if (_deleteStats != null) _deleteStats.Reset();
            if (_evictPerSecStats != null) _evictPerSecStats.Reset();
            if (_expiryPerSecStats != null) _expiryPerSecStats.Reset();
            if (_stateTxfrPerSecStats != null) _stateTxfrPerSecStats.Reset();
            if (_dataBalPerSecStats != null) _dataBalPerSecStats.Reset();
            if (_mirrorQueueSizeStats != null) _mirrorQueueSizeStats.Reset();
            if (_slidingIndexQueueSizeStats != null) _slidingIndexQueueSizeStats.Reset();
            if (_msecPerGetAvgStats != null) _msecPerGetAvgStats.Reset();
            if (_msecPerAddAvgStats != null) _msecPerAddAvgStats.Reset();
            if (_msecPerUpdateAvgStats != null) _msecPerUpdateAvgStats.Reset();
            if (_msecPerDeleteAvgStats != null) _msecPerDeleteAvgStats.Reset();
            if (_usageMsecPerGetStats != null) _usageMsecPerGetStats.Reset();
            if (_usageMsecPerAddStats != null) _usageMsecPerAddStats.Reset();
            if (_usageMsecPerUpdateStats != null) _usageMsecPerUpdateStats.Reset();
            if (_usageMsecPerDeleteStats != null) _usageMsecPerDeleteStats.Reset();
            if (_expirationIndexSizeStats != null) _expirationIndexSizeStats.Reset();
            if (_evictionIndexSizeStats != null) _evictionIndexSizeStats.Reset();
            if (_messageCountStats != null) _messageCountStats.Reset();
            if (_topicCountStats != null) _topicCountStats.Reset();
            if (_messageStoreSizeStats != null) _messageStoreSizeStats.Reset();
            if (_messagePublishPerSecStats != null) _messagePublishPerSecStats.Reset();
            if (_messageDeliverPerSecStats != null) _messageDeliverPerSecStats.Reset();
            if (_messageExpiredPerSecStats != null) _messageExpiredPerSecStats.Reset();
            if (_pubsubCounterList != null) _pubsubCounterList = null;
        }

        public double GetCounterValue(string counterName)
        {
            double value = 0.0;
            PerformanceCounterBase counter = null;

            switch (counterName)
            {
                case CounterNames.Count:
                    counter = _countStats;
                    break;
                case CounterNames.CacheSize:
                    counter = _sizeStats;
                    break;
                case CounterNames.AddPerSec:
                    counter = _addStats;
                    break;
                case CounterNames.UpdPerSec:
                    counter = _updateStats;
                    break;
                case CounterNames.GetPerSec:
                    counter = _fetchStats;
                    break;
                case CounterNames.HitsPerSec:
                    counter = _hitStats;
                    break;
                case CounterNames.MissPerSec:
                    counter = _missStats;
                    break;
                case CounterNames.CacheLastAccessCount:
                    counter = _cacheLastAccessCountStats;
                    break;
                case CounterNames.HitsRatioSec:
                    counter = _hitsRatioSecStats;
                    break;
                case CounterNames.DelPerSec:
                    counter = _deleteStats;
                    break;
                case CounterNames.EvictPerSec:
                    counter = _evictPerSecStats;
                    break;
                case CounterNames.ExpiryPerSec:
                    counter = _expiryPerSecStats;
                    break;
                case CounterNames.StateTxfrPerSec:
                    counter = _stateTxfrPerSecStats;
                    break;
                case CounterNames.DataBalPerSec:
                    counter = _dataBalPerSecStats;
                    break;
                case CounterNames.MsecPerGetAvg:
                    counter = _msecPerGetAvgStats;
                    break;
                case CounterNames.MsecPerAddAvg:
                    counter = _msecPerAddAvgStats;
                    break;
                case CounterNames.MsecPerUpdAvg:
                    counter = _msecPerUpdateAvgStats;
                    break;
                case CounterNames.MsecPerDelAvg:
                    counter = _msecPerDeleteAvgStats;
                    break;
                case CounterNames.MirrorQueueSize:
                    counter = _mirrorQueueSizeStats;
                    break;
                case CounterNames.SlidingIndexQueueSize:
                    counter = _slidingIndexQueueSizeStats;
                    break;
                case CounterNames.EvictionIndexSize:
                    counter = _evictionIndexSizeStats;
                    break;
                case CounterNames.ExpirationIndexSize:
                    counter = _expirationIndexSizeStats;
                    break;
                case CounterNames.MessageCount:
                    counter = _messageCountStats;
                    break;
                case CounterNames.TopicCount:
                    counter = _topicCountStats;
                    break;
                case CounterNames.MessageStoreSize:
                    counter = _messageStoreSizeStats;
                    break;
                case CounterNames.MessagePublishPerSec:
                    counter = _messagePublishPerSecStats;
                    break;
                case CounterNames.MessageDeliveryPerSec:
                    counter = _messageDeliverPerSecStats;
                    break;
                case CounterNames.MessageExpiredPerSec:
                    counter = _messageExpiredPerSecStats;
                    break;
               
            }

            if (counter != null)
            {
                lock (counter)
                {
                    value = counter.Value;
                }
            }

            return value;
        }

        public string GetInstanceName(string instanceName, int port, bool inProc)
        {
            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
        }

        public void IncrementAddPerSecStats()
        {
            if (_addStats != null) lock (_addStats) _addStats.Increment();
        }

        public void IncrementByAddPerSecStats(long value)
        {
            if (_addStats != null) lock (_addStats) _addStats.IncrementBy(value);
        }

        public void IncrementByDelPerSecStats(long value)
        {
            if (_deleteStats != null) lock (_deleteStats) { _deleteStats.IncrementBy(value); }
        }

        public void IncrementByGetPerSecStats(long value)
        {
            if (_fetchStats != null) lock (_fetchStats) _fetchStats.IncrementBy(value);
        }

        public void IncrementByHitsPerSecStats(long value)
        {
            if (_hitStats != null) lock (_hitStats) _hitStats.IncrementBy(value);
        }

        public void IncrementByHitsRatioPerSecBaseStats(long value)
        {
        }

        public void IncrementHitsRatioPerSecBaseStats()
        {
        }

        public void IncrementByHitsRatioPerSecStats(long value)
        {
            if (_hitsRatioSecStats != null)
            {
                lock (_hitsRatioSecStats)
                {
                    _hitsRatioSecStats.IncrementBy(value);
                }
            }
        }

        public void IncrementByMissPerSecStats(long value)
        {
            if (_missStats != null) lock (_missStats) _missStats.IncrementBy(value);
        }

        public void IncrementByUpdPerSecStats(long value)
        {
            if (_updateStats != null) lock (_updateStats) _updateStats.IncrementBy(value);
        }

        public void IncrementCacheLastAccessCountStats(long count)
        {
            if (_cacheLastAccessCountStats != null)
            {
                lock (_cacheLastAccessCountStats)
                {
                    _cacheLastAccessCountStats.Value = count;
                }
            }
        }

        public void IncrementCacheSize()
        {
            if (_sizeStats != null) lock (_sizeStats) _sizeStats.Increment();
        }

        public void IncrementCacheSizeBy(long value)
        {
            if (_sizeStats != null) lock (_sizeStats) _sizeStats.IncrementBy(value);
        }

        public void IncrementCountStats(long count)
        {
            if (_countStats != null) lock (_countStats) _countStats.Value = count;
        }

        public void IncrementCountStatsBy(long count)
        {
            if (_countStats != null) lock (_countStats) _countStats.IncrementBy(count);
        }

        public void IncrementDataBalPerSecStats()
        {
            Interlocked.Exchange(ref this._dataBalance, (this._dataBalance + 1));
            if (_dataBalPerSecStats != null) lock (_dataBalPerSecStats) { _dataBalPerSecStats.Increment(); }
        }

        public void IncrementDataBalPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._dataBalance, (this._dataBalance + value));
            if (_dataBalPerSecStats != null) lock (_dataBalPerSecStats) { _dataBalPerSecStats.IncrementBy(value); }
        }

        public void IncrementDelPerSecStats()
        {
            if (_deleteStats != null) lock (_deleteStats) { _deleteStats.Increment(); }
        }

        public void IncrementDelPerSecStats(int value)
        {
            if (_deleteStats != null) lock (_deleteStats) { _deleteStats.IncrementBy(value); }
        }

        public void IncrementEvictionIndexSize()
        {
            if (_evictionIndexSizeStats != null)
            {
                lock (_evictionIndexSizeStats)
                {
                    IncrementEvictionIndexSizeBy(1);
                }
            }
        }

        public void IncrementEvictionIndexSizeBy(long value)
        {
            if (_evictionIndexSizeStats != null)
            {
                lock (_evictionIndexSizeStats)
                {
                    _evictionIndexSizeStats.IncrementBy(value);
                }
            }
        }

        public void IncrementEvictPerSecStats()
        {
            Interlocked.Exchange(ref this._evictions, (this._evictions + 1));
            if (_evictPerSecStats != null) lock (_evictPerSecStats) { _evictPerSecStats.Increment(); }
        }

        public void IncrementEvictPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._evictions, (this._evictions + value));
            if (_evictPerSecStats != null) lock (_evictPerSecStats) { _evictPerSecStats.IncrementBy(value); }
        }

        public void IncrementExpiryPerSecStats()
        {
            Interlocked.Exchange(ref this._expirations, (this._expirations + 1));
            if (_expiryPerSecStats != null) lock (_expiryPerSecStats) { _expiryPerSecStats.Increment(); }
        }

        public void IncrementExpiryPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._expirations, (this._expirations + value));
            if (_expiryPerSecStats != null) lock (_expiryPerSecStats) { _expiryPerSecStats.IncrementBy(value); }
        }

        public void IncrementGetPerSecStats()
        {
            if (_fetchStats != null) lock (_fetchStats) _fetchStats.Increment();
        }


        public void IncrementHitsPerSecStats()
        {
            if (_hitStats != null) lock (_hitStats) _hitStats.Increment();
        }

        public void IncrementHitsRatioPerSecStats()
        {
            if (_hitsRatioSecStats != null)
            {
                lock (_hitsRatioSecStats)
                {
                    _hitsRatioSecStats.Increment();
                }
            }
        }

        public void IncrementMessageDeliverPerSec()
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageDeliveryPerSec))
            {
                PerformanceCounterBase performanceCounterBase = _pubsubCounterList[CounterNames.MessageDeliveryPerSec];
                if (performanceCounterBase != null)
                {
                    lock (performanceCounterBase)
                    {
                        performanceCounterBase.Increment();
                    }
                }
            }
        }

        public void IncrementMessageDeliverPerSec(long value)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageDeliveryPerSec))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[CounterNames.MessageDeliveryPerSec];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value);
                    }
                }
            }
        }

        public void IncrementMessageExpiredPerSec(long value)
        {

            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(CounterNames.MessageExpiredPerSec))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[CounterNames.MessageExpiredPerSec];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value);
                    }
                }
            }
        }

        public void IncrementMessagePublishedPerSec()
        {
            PubSubCounterSetter(CounterNames.MessagePublishPerSec, PerformanceOperator.Increment);
        }

        public void IncrementMirrorQueueSizeStats(long count)
        {
            if (_mirrorQueueSizeStats != null)
            {
                lock (_mirrorQueueSizeStats)
                {
                    _mirrorQueueSizeStats.Value = count;
                }
            }
        }

        public void IncrementMissPerSecStats()
        {
            if (_missStats != null) lock (_missStats) _missStats.Increment();
        }

        public void IncrementMsecPerMessagePublish(long count)
        {
            if (_mirrorQueueSizeStats != null)
            {
                lock (_mirrorQueueSizeStats)
                {
                    _mirrorQueueSizeStats.Value = count;
                }
            }
        }

        public void IncrementSlidingIndexQueueSizeStats(long count)
        {
            if (_slidingIndexQueueSizeStats != null)
            {
                lock (_slidingIndexQueueSizeStats)
                {
                    _slidingIndexQueueSizeStats.Value = count;
                }
            }
        }

        /// <summary>Resets the State Transfer perfmon counter value</summary>
        public void ResetStateTransferPerfmonCounter()
        {
            if (_stateTxfrPerSecStats != null) lock (_stateTxfrPerSecStats) { _stateTxfrPerSecStats.Reset(); }
        }

        public void IncrementStateTxfrPerSecStats()
        {
            Interlocked.Exchange(ref this._stateXfer, (this._stateXfer + 1));
            if (_stateTxfrPerSecStats != null) lock (_stateTxfrPerSecStats) { _stateTxfrPerSecStats.Increment(); }
        }

        public void IncrementStateTxfrPerSecStatsBy(long value)
        {
            Interlocked.Exchange(ref this._stateXfer, (this._stateXfer + value));
            if (_stateTxfrPerSecStats != null) lock (_stateTxfrPerSecStats) { _stateTxfrPerSecStats.IncrementBy(value); }
        }

        public void IncrementUpdPerSecStats()
        {
            if (_updateStats != null) lock (_updateStats) _updateStats.Increment();
        }

        public void MsecPerAddBeginSample()
        {
            if (_msecPerAddAvgStats != null) lock (_msecPerAddAvgStats) { _usageMsecPerAddStats.BeginSample(); }
        }

        public void MsecPerAddEndSample()
        {
            if (_msecPerAddAvgStats != null)
            {
                lock (_msecPerAddAvgStats)
                {
                    _usageMsecPerAddStats.EndSample();
                    _msecPerAddAvgStats.IncrementBy(_usageMsecPerAddStats.Current * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                }
            }
        }

        public void MsecPerDelBeginSample()
        {
            if (_msecPerDeleteAvgStats != null) lock (_msecPerDeleteAvgStats) { _usageMsecPerDeleteStats.BeginSample(); }
        }

        public void MsecPerDelEndSample()
        {
            if (_msecPerDeleteAvgStats != null)
            {
                lock (_msecPerDeleteAvgStats)
                {
                    _usageMsecPerDeleteStats.EndSample();
                    _msecPerDeleteAvgStats.IncrementBy(_usageMsecPerDeleteStats.Current * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                }
            }
        }

        public void MsecPerGetBeginSample()
        {
            if (_msecPerGetAvgStats != null) lock (_msecPerGetAvgStats) { _usageMsecPerGetStats.BeginSample(); }
        }

        public void MsecPerGetEndSample()
        {
            if (_msecPerGetAvgStats != null)
            {
                lock (_msecPerGetAvgStats)
                {
                    _usageMsecPerGetStats.EndSample();
                    _msecPerGetAvgStats.IncrementBy(_usageMsecPerGetStats.Current * 1000000 / Stopwatch.Frequency);// ts.Milliseconds);
                }
            }
        }

        public void MsecPerUpdBeginSample()
        {
            if (_msecPerUpdateAvgStats != null)
                lock (_msecPerUpdateAvgStats) { _usageMsecPerUpdateStats.BeginSample(); }
        }

        public void MsecPerUpdEndSample()
        {
            if (_msecPerUpdateAvgStats != null)
            {
                lock (_msecPerUpdateAvgStats)
                {
                    _usageMsecPerUpdateStats.EndSample();
                    _msecPerUpdateAvgStats.IncrementBy(_usageMsecPerUpdateStats.Current * 1000000 / Stopwatch.Frequency);//ts.Milliseconds);
                }
            }
        }

        public void PubSubCounterSetter(string counterType, PerformanceOperator counterOperation)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.Count > 0 && _pubsubCounterList.ContainsKey(counterType))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[counterType];
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
                                if (performanceCounter.Value != 0)
                                    performanceCounter.Decrement();
                                break;
                        }
                    }
                }
            }
        }

        public void SetCacheSize(long value)
        {
            if (_sizeStats != null)
                _sizeStats.Value = value;
        }

        public void SetEvictionIndexSize(long value)
        {
            if (_evictionIndexSizeStats != null)
            {
                lock (_evictionIndexSizeStats)
                {
                    _evictionIndexSizeStats.Value = value;

                }
            }
        }

        public void SetExpirationIndexSize(long value)
        {
            if (_expirationIndexSizeStats != null)
            {
                lock (_expirationIndexSizeStats)
                {
                    _expirationIndexSizeStats.Value = value;
                }
            }
        }

        public void SetPubSubCounter(long value, string counterType)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(counterType))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[counterType];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.Value = value;
                    }
                }
            }
        }
        


        public void DataTypeCounterSetter(string counterType, long size)
        {

        }

       

    }
}
