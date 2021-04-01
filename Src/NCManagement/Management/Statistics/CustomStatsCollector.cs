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
using Alachisoft.NCache.Common.Caching.Statistics.CustomCounters;
using Alachisoft.NCache.Common.Net;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using Alachisoft.NCache.Management;
using Alachisoft.NCache.Management.ClientConfiguration.Dom;
using Alachisoft.NCache.Management.Management;
using Alachisoft.NCache.Management.RPC;
using Alachisoft.NCache.Management.ServiceControl;
using Alachisoft.NCache.ServiceControl;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace Alachisoft.NCache.Management.Statistics
{
    public class CustomStatsCollector : IDisposable, StatisticsCounter
    {
        /// <summary> Instance name. </summary>
        private string _instanceName;
        /// <summary> Port number. </summary>
        private string _port;

        private string _cacheID;

        /// <summary> performance counter for cache requests per second by the client. </summary>
        private PerformanceCounterBase _pcClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the client. </summary>
        private PerformanceCounterBase _pcClientResponsesPerSec = null;
        /// <summary> performance counter for cache requests per second by all the clients. </summary>
        private PerformanceCounterBase _pcTotalClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the all clients. </summary>
        private PerformanceCounterBase _pcTotalClientResponsesPerSec = null;

        /// <summary> performance counter for Cache get operations per second. </summary>
        private PerformanceCounterBase _pcGetPerSec = null;
        /// <summary> performance counter for Cache add operations per second. </summary>
        private PerformanceCounterBase _pcAddPerSec = null;
        /// <summary> performance counter for Cache update operations per second. </summary>
        private PerformanceCounterBase _pcUpdPerSec = null;
        /// <summary> performance counter for Cache remove operations per second. </summary>
        private PerformanceCounterBase _pcDelPerSec = null;
        /// <summary> performance counter read operations per second </summary>
        private PerformanceCounterBase _pcReadOperationsPerSec = null;
        /// <summary> performance counter write operations per second </summary>
        private PerformanceCounterBase _pcWriteOperationsPerSec = null;
        /// <summary> performance counter for Mirror Queue size. </summary>
        private PerformanceCounterBase _pcReqrQueueSize = null;

        /// <summary> performance counter for Cache avg. per milli-second time of get operations. </summary>
        private AverageCounter _pcMsecPerGetAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of add operations. </summary>
        private AverageCounter _pcMsecPerAddAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of update operations. </summary>
        private AverageCounter _pcMsecPerUpdAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of remove operations. </summary>
        private AverageCounter _pcMsecPerDelAvg = null;
        /// <summary> performance counter for Cache avg. per milli-second time of Event operations. </summary>
        private AverageCounter _pcMsecPerEventAvg = null;
        /// <summary> performance counter for events processed per second. </summary>
        private PerformanceCounterBase _pcEventProcesedPerSec = null;
        /// <summary> performance counter for events Triggered/Received per second. </summary>
        private PerformanceCounterBase _pcEventTriggeredPerSec = null;

        //Bulk Counters
        private AverageCounter _pcMsecPerAddBulkAvg = null;
        private AverageCounter _pcMsecPerUpdBulkAvg = null;
        private AverageCounter _pcMsecPerGetBulkAvg = null;
        private AverageCounter _pcMsecPerDelBulkAvg = null;

        /// <summary> performance counter for</summary>
        private AverageCounter _pcAvgItemSize = null;
        /// <summary> usage statistics for Cache per milli-second time of get operations. </summary>
        private UsageStats _usMsecPerGet = null;
        /// <summary> usage statistics for Cache per milli-second time of add operations. </summary>
        private UsageStats _usMsecPerAdd = null;
        /// <summary> usage statistics for Cache per milli-second time of update operations. </summary>
        private UsageStats _usMsecPerUpd = null;
        /// <summary> usage statistics for Cache per milli-second time of remove operations. </summary>
        private UsageStats _usMsecPerDel = null;
        /// <summary> usage statistics for Cache per milli-second time of event operations. </summary>
        private UsageStats _usMsecPerEvent = null;

        private AverageCounter _pcMsecPerSerializationAvg = null;

        private AverageCounter _pcMsecPerDeserializationAvg = null;

        private AverageCounter _pcMsecPerMessagePublishAvg = null;
        private PerformanceCounterBase _pcMessagePublishPerSec = null;
        private PerformanceCounterBase _pcMessageDeliverPerSec = null;
        private Dictionary<string, PerformanceCounterBase> _pubsubCounterList = null;


        /// <summary> performance counter for Cache max. per milli-second time of get operations. </summary>
        private PerformanceCounterBase _pcMsecPerGetBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of add operations. </summary>
        private PerformanceCounterBase _pcMsecPerAddBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of update operations. </summary>
        private PerformanceCounterBase _pcMsecPerUpdBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of remove operations. </summary>
        private PerformanceCounterBase _pcMsecPerDelBase = null;
        /// <summary> performance counter for Cache max. per milli-second time of Event operations. </summary>
        private PerformanceCounterBase _pcMsecPerEventBase = null;

        private PerformanceCounterBase _pcMsecPerAddBulkBase = null;
        private PerformanceCounterBase _pcMsecPerUpdBulkBase = null;
        private PerformanceCounterBase _pcMsecPerGetBulkBase = null;
        private PerformanceCounterBase _pcMsecPerDelBulkBase = null;
        /// <summary> base performance counter for</summary>
        private PerformanceCounterBase _pcAvgItemSizeBase = null;
        private PerformanceCounterBase _pcMsecPerSerializationAvgBase = null;
        private PerformanceCounterBase _pcMsecPerDeserializationAvgBase = null;
        private PerformanceCounterBase _pcMsecPerMessagePublishBase = null;

        private Thread _publishingThread;
        private bool _publish;
        private DateTime _lastPublish = DateTime.Now;
        private const int _publishingInterval = 1;  // publishing counters interval is in seconds; i.e. 1 seconds
        private readonly object _lockObj = new object();
        private long _version;
        NCacheRPCService NCache = new NCacheRPCService(null);
        ICacheServer _cacheServer = null;

        private bool _isEnabled = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public CustomStatsCollector(string instanceName, int port, string cacheId)
        {
            _port = ":" + port.ToString();
            _instanceName = GetInstanceName(instanceName, port, false);
            _cacheID = cacheId;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        public CustomStatsCollector(string instanceName, bool inProc)
        {
            _cacheID = instanceName;
            _instanceName = GetInstanceName(instanceName, 0, inProc);

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
            get { return true; }
        }



        #region	/                 --- IDisposable ---           /

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or 
        /// resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            _pcClientRequestsPerSec.Reset();
            _pcClientResponsesPerSec.Reset();
            _pcTotalClientRequestsPerSec.Reset();
            _pcTotalClientResponsesPerSec.Reset();
            _pcAddPerSec.Reset();
            _pcGetPerSec.Reset();
            _pcUpdPerSec.Reset();
            _pcDelPerSec.Reset();

            _pcEventTriggeredPerSec.Reset();
            _pcEventProcesedPerSec.Reset();

            _pcReadOperationsPerSec.Reset();
            _pcWriteOperationsPerSec.Reset();


            //Bulk Counters
            _pcMsecPerAddBulkAvg.Reset();
            _pcMsecPerGetBulkAvg.Reset();
            _pcMsecPerUpdBulkAvg.Reset();
            _pcMsecPerDelBulkAvg.Reset();
            _pcMsecPerGetAvg.Reset();
            _usMsecPerGet = new UsageStats();
            _pcMsecPerAddAvg.Reset();
            _usMsecPerAdd.Reset();
            _pcMsecPerUpdAvg.Reset();
            _usMsecPerUpd.Reset();
            _pcMsecPerDelAvg.Reset();
            _usMsecPerDel.Reset();
            _pcReqrQueueSize.Reset();
            _pcAvgItemSize.Reset();

            _pcMsecPerEventAvg.Reset();
            _usMsecPerEvent.Reset();

            _usMsecPerDel.Reset();

            _pcMsecPerSerializationAvg.Reset();
            _pcMsecPerDeserializationAvg.Reset();

            _pcMsecPerMessagePublishAvg.Reset();
            _pcMessagePublishPerSec.Reset();
            _pcMessageDeliverPerSec.Reset();

            _pcMsecPerAddBase.Reset();
            _pcMsecPerDelBase.Reset();
            _pcMsecPerGetBase.Reset();
            _pcMsecPerUpdBase.Reset();
            _pcMsecPerAddBulkBase.Reset();
            _pcMsecPerGetBulkBase.Reset();
            _pcMsecPerUpdBulkBase.Reset();
            _pcMsecPerDelBulkBase.Reset();
            _pcAvgItemSizeBase.Reset();
            _pcMsecPerEventBase.Reset();
            _pcMsecPerSerializationAvgBase.Reset();
            _pcMsecPerDeserializationAvgBase.Reset();

            if (_pubsubCounterList != null)
            {
                foreach (PerformanceCounterBase counter in _pubsubCounterList.Values)
                {
                    if (counter != null)
                    {
                        counter.Reset();
                    }
                }

                _pubsubCounterList.Clear();
                _pubsubCounterList = null;

            }

            StopPublishing();
        }


        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        public void InitializePerfCounters(bool inproc)
        {
            _pcClientRequestsPerSec = new RateOfCounter(CustomCounterNames.ClientRequestsPerSec, _instanceName);
            _pcClientResponsesPerSec = new RateOfCounter(CustomCounterNames.ClientResponsesPerSec, _instanceName);
            _pcTotalClientRequestsPerSec = new RateOfCounter(CustomCounterNames.TotalClientRequestsPerSec, CustomCounterNames.TotalClientStats);
            _pcTotalClientResponsesPerSec = new RateOfCounter(CustomCounterNames.TotalClientResponsesPerSec, CustomCounterNames.TotalClientStats);
            _pcAddPerSec = new RateOfCounter(CustomCounterNames.AddPerSec, _instanceName);
            _pcGetPerSec = new RateOfCounter(CustomCounterNames.FetchPerSec, _instanceName);
            _pcUpdPerSec = new RateOfCounter(CustomCounterNames.UpdatePerSec, _instanceName);
            _pcDelPerSec = new RateOfCounter(CustomCounterNames.DeletePerSec, _instanceName);

            _pcEventTriggeredPerSec = new RateOfCounter(CustomCounterNames.EventsTriggeredPerSec, _instanceName);
            _pcEventProcesedPerSec = new RateOfCounter(CustomCounterNames.EventProcesedPerSec, _instanceName);

            _pcReadOperationsPerSec = new RateOfCounter(CustomCounterNames.ReadOperationsPerSec, _instanceName);
            _pcWriteOperationsPerSec = new RateOfCounter(CustomCounterNames.WriteOperationsPerSec, _instanceName);


            //Bulk Counters
            _pcMsecPerAddBulkAvg = new AverageCounter(CustomCounterNames.MsecPerAddBulkAvg, _instanceName);
            _pcMsecPerGetBulkAvg = new AverageCounter(CustomCounterNames.MsecPerGetBulkAvg, _instanceName);
            _pcMsecPerUpdBulkAvg = new AverageCounter(CustomCounterNames.MsecPerUpdBulkAvg, _instanceName);
            _pcMsecPerDelBulkAvg = new AverageCounter(CustomCounterNames.MsecPerDelBulkAvg, _instanceName);
            _pcMsecPerGetAvg = new AverageCounter(CustomCounterNames.MsecPerGetAvg, _instanceName);
            _usMsecPerGet = new UsageStats();
            _pcMsecPerAddAvg = new AverageCounter(CustomCounterNames.MsecPerAddAvg, _instanceName);
            _usMsecPerAdd = new UsageStats();
            _pcMsecPerUpdAvg = new AverageCounter(CustomCounterNames.MsecPerUpdAvg, _instanceName);
            _usMsecPerUpd = new UsageStats();
            _pcMsecPerDelAvg = new AverageCounter(CustomCounterNames.MsecPerDelAvg, _instanceName);
            _usMsecPerDel = new UsageStats();

            _pcReqrQueueSize = new NumberOfItemCounter(CustomCounterNames.ReqrQueueSize, _instanceName);
            _pcAvgItemSize = new AverageCounter(CustomCounterNames.AvgItemSize, _instanceName);

            _pcMsecPerEventAvg = new AverageCounter(CustomCounterNames.MsecPerEventAvg, _instanceName);
            _usMsecPerEvent = new UsageStats();

            _usMsecPerDel = new UsageStats();

            _pcMsecPerSerializationAvg = new AverageCounter(CustomCounterNames.MsecPerSerializationAvg, _instanceName);
            _pcMsecPerDeserializationAvg = new AverageCounter(CustomCounterNames.MsecPerDeserializationAvg, _instanceName);

            _pubsubCounterList = new Dictionary<string, PerformanceCounterBase>();

            _pcMsecPerMessagePublishAvg = new AverageCounter(CustomCounterNames.AvgPublishMessage, _instanceName);
            _pcMessagePublishPerSec = new RateOfCounter(CustomCounterNames.MessagePublishPerSec, _instanceName);
            _pcMessageDeliverPerSec = new RateOfCounter(CustomCounterNames.MessageDeliveryPerSec, _instanceName);
            _pubsubCounterList.Add(CounterNames.MessagePublishPerSec, _pcMessagePublishPerSec);
            _pubsubCounterList.Add(CounterNames.AvgPublishMessage, _pcMsecPerMessagePublishAvg);
            _pubsubCounterList.Add(CounterNames.MessageDeliveryPerSec, _pcMessageDeliverPerSec);

            _pcMsecPerAddBulkBase = new RateOfCounter(CustomCounterNames.MsecPerAddBulkBase, _instanceName);
            _pcMsecPerGetBulkBase = new RateOfCounter(CustomCounterNames.MsecPerGetBulkBase, _instanceName);
            _pcMsecPerUpdBulkBase = new RateOfCounter(CustomCounterNames.MsecPerUpdBulkBase, _instanceName);
            _pcMsecPerDelBulkBase = new RateOfCounter(CustomCounterNames.MsecPerDelBulkBase, _instanceName);
            _pcMsecPerGetBase = new RateOfCounter(CustomCounterNames.MsecPerGetBase, _instanceName);
            _pcMsecPerAddBase = new RateOfCounter(CustomCounterNames.MsecPerAddBase, _instanceName);
            _pcMsecPerUpdBase = new RateOfCounter(CustomCounterNames.MsecPerUpdBase, _instanceName);
            _pcMsecPerDelBase = new RateOfCounter(CustomCounterNames.MsecPerDelBase, _instanceName);
            _pcAvgItemSizeBase = new AverageCounter(CustomCounterNames.AvgItemSizeBase, _instanceName);
            _pcMsecPerEventBase = new RateOfCounter(CustomCounterNames.MsecPerEventBase, _instanceName);
            _pcMsecPerSerializationAvgBase = new RateOfCounter(CustomCounterNames.MsecPerSerializationAvgBase, _instanceName);
            _pcMsecPerDeserializationAvgBase = new RateOfCounter(CustomCounterNames.MsecPerDeserializationAvgBase, _instanceName);
            _pcMsecPerMessagePublishBase = new RateOfCounter(CounterNames.AvgPublishMessageBase, _instanceName);
            _pubsubCounterList.Add(CounterNames.AvgPublishMessageBase, _pcMsecPerMessagePublishAvg);

            foreach (var item in _pubsubCounterList.Values)
            {
                item.Value = 0;
            }
        }

        #endregion

        /// <summary>
        /// Gets or Sets the value indicating whether Performance Stats collection is enabled or not.
        /// On initialize Performance Collection is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { _isEnabled = value; }
        }

        /// <summary> 
        /// Increment the performance counter for Requests Per second by client. 
        /// </summary>
        public void IncrementClientRequestsPerSecStats(long requests)
        {
            if (_pcClientRequestsPerSec != null)
            {
                lock (_pcClientRequestsPerSec)
                {
                    _pcClientRequestsPerSec.IncrementBy(requests);
                    IncrementTotalClientRequestsPerSecStats(requests);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by client. 
        /// </summary>
        public void IncrementClientResponsesPerSecStats(long responses)
        {
            if (_pcClientResponsesPerSec != null)
            {
                lock (_pcClientResponsesPerSec)
                {
                    _pcClientResponsesPerSec.IncrementBy(responses);
                    IncrementTotalClientResponsesPerSecStats(responses);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Requests Per second by all the clients.
        /// </summary>
        public void IncrementTotalClientRequestsPerSecStats(long requests)
        {
            if (_pcTotalClientRequestsPerSec != null)
            {
                lock (_pcTotalClientRequestsPerSec)
                {
                    _pcTotalClientRequestsPerSec.IncrementBy(requests);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        public void IncrementTotalClientResponsesPerSecStats(long responses)
        {
            if (_pcTotalClientResponsesPerSec != null)
            {
                lock (_pcTotalClientResponsesPerSec)
                {
                    _pcTotalClientResponsesPerSec.IncrementBy(responses);
                }
            }
        }


        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        public void IncrementGetPerSecStats()
        {
            if (_pcGetPerSec != null)
            {
                lock (_pcGetPerSec)
                {
                    _pcGetPerSec.Increment();
                }
            }

            if (_pcReadOperationsPerSec != null)
            {
                lock (_pcReadOperationsPerSec)
                {
                    _pcReadOperationsPerSec.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache get operations per second. 
        /// </summary>
        public void IncrementByGetPerSecStats(long value)
        {
            if (_pcGetPerSec != null)
            {
                lock (_pcGetPerSec)
                {
                    _pcGetPerSec.IncrementBy(value);
                }
            }

            if (_pcReadOperationsPerSec != null)
            {
                lock (_pcReadOperationsPerSec)
                {
                    _pcReadOperationsPerSec.IncrementBy(value);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        public void IncrementAddPerSecStats()
        {
            if (_pcAddPerSec != null)
            {
                lock (_pcAddPerSec)
                {
                    _pcAddPerSec.Increment();
                }
            }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache add operations per second. 
        /// </summary>
        public void IncrementByAddPerSecStats(long value)
        {
            if (_pcAddPerSec != null)
            {
                lock (_pcAddPerSec)
                {
                    _pcAddPerSec.IncrementBy(value);
                }
            }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.IncrementBy(value);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementUpdPerSecStats()
        {
            if (_pcUpdPerSec != null) lock (_pcUpdPerSec) { _pcUpdPerSec.Increment(); }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementByUpdPerSecStats(long value)
        {
            if (_pcUpdPerSec != null) lock (_pcUpdPerSec) { _pcUpdPerSec.IncrementBy(value); }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.IncrementBy(value);
                }
            }
        }


        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementDelPerSecStats()
        {
            if (_pcDelPerSec != null) lock (_pcDelPerSec) { _pcDelPerSec.Increment(); }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.Increment();
                }
            }
        }

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementByDelPerSecStats(long value)
        {
            if (_pcDelPerSec != null) lock (_pcDelPerSec) { _pcDelPerSec.IncrementBy(value); }

            if (_pcWriteOperationsPerSec != null)
            {
                lock (_pcWriteOperationsPerSec)
                {
                    _pcWriteOperationsPerSec.IncrementBy(value);
                }
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// event operations. 
        /// </summary>s
        public void IncrementAvgEventProcessingSample(long value)
        {
            if (_pcMsecPerEventAvg != null)
            {
                lock (_pcMsecPerEventAvg)
                {
                    long val = (value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerEventAvg.IncrementBy(val);
                    _pcMsecPerEventBase.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        public void IncrementEventsProcessedPerSec()
        {
            if (_pcEventProcesedPerSec != null)
            {
                lock (_pcEventProcesedPerSec)
                {
                    _pcEventProcesedPerSec.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Responses received Per second by all the clients. 
        /// </summary>
        public void IncrementEventsTriggeredPerSeconds()
        {
            if (_pcEventTriggeredPerSec != null)
            {
                lock (_pcEventTriggeredPerSec)
                {
                    _pcEventTriggeredPerSec.Increment();
                }
            }
        }

        /// <summary>
        /// Increments the performance counter for Average �s/serialization.
        /// </summary>
        /// <param name="value">Value to increment by</param>
        public void IncrementMsecPerSerialization(long value)
        {
            if (_pcMsecPerSerializationAvg != null)
            {
                lock (_pcMsecPerSerializationAvg)
                {
                    _pcMsecPerSerializationAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerSerializationAvgBase.Increment();
                }
            }
        }


        /// <summary>
        /// Increments the performance counter for Average �s/serialization.
        /// </summary>
        /// <param name="value">Value to increment by</param>
        public void IncrementMsecPerDeserialization(long value)
        {
            if (_pcMsecPerDeserializationAvg != null)
            {
                lock (_pcMsecPerDeserializationAvg)
                {
                    _pcMsecPerDeserializationAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerDeserializationAvgBase.Increment();
                }
            }
        }


        /// <summary>
        /// Increments the performance counter for Average Item Size.
        /// </summary>
        public void IncrementAvgItemSize(long itemSize)
        {
            if (_pcAvgItemSize != null)
            {
                lock (_pcAvgItemSize)
                {
                    _pcAvgItemSize.IncrementBy(itemSize);
                    _pcAvgItemSizeBase.Increment();
                }
            }
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// fetch operations. 
        /// </summary>
        public void MsecPerGetBeginSample()
        {
            if (_pcMsecPerGetAvg != null) lock (_usMsecPerGet) { _usMsecPerGet.BeginSample(); }
        }
        public void IncrementMsecPerGetSample(long value)
        {
            if (_pcMsecPerDelAvg != null)
            {
                lock (_pcMsecPerGetAvg)
                {
                    _pcMsecPerGetAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerGetBase.Increment();
                }
            }
        }

        public void IncrementMsecPerGetSample(long value, long baseValue)
        {
            if (_pcMsecPerDelAvg != null)
            {
                lock (_pcMsecPerGetAvg)
                {
                    _pcMsecPerGetAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerGetBase.IncrementBy(baseValue);
                }
            }
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
                    _pcMsecPerGetAvg.IncrementBy(_usMsecPerGet.Current * 1000000 / Stopwatch.Frequency);// ts.Milliseconds);
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
        public void IncrementMsecPerAddSample(long value)
        {
            if (_pcMsecPerAddAvg != null)
            {
                lock (_pcMsecPerAddAvg)
                {
                    _pcMsecPerAddAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerAddBase.Increment();
                }
            }
        }

        public void IncrementMsecPerAddSample(long value, long baseValue)
        {
            if (_pcMsecPerAddAvg != null)
            {
                lock (_pcMsecPerAddAvg)
                {
                    _pcMsecPerAddAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerAddBase.IncrementBy(baseValue);
                }
            }
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
                    _pcMsecPerAddAvg.IncrementBy(_usMsecPerAdd.Current * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerAddBase.Increment();
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
        public void IncrementMsecPerUpdSample(long value)
        {
            if (_pcMsecPerUpdAvg != null)
            {
                lock (_pcMsecPerUpdAvg)
                {
                    _pcMsecPerUpdAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerUpdBase.Increment();
                }
            }
        }

        public void IncrementMsecPerUpdSample(long value, long baseValue)
        {
            if (_pcMsecPerUpdAvg != null)
            {
                lock (_pcMsecPerUpdAvg)
                {
                    _pcMsecPerUpdAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerUpdBase.IncrementBy(baseValue);
                }
            }
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
                    _pcMsecPerUpdAvg.IncrementBy(_usMsecPerUpd.Current * 1000000 / Stopwatch.Frequency);//ts.Milliseconds);
                    _pcMsecPerUpdBase.Increment();
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
        public void IncrementMsecPerDelSample(long value)
        {
            if (_pcMsecPerDelAvg != null)
            {
                lock (_pcMsecPerDelAvg)
                {
                    _pcMsecPerDelAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerDelBase.Increment();
                }
            }
        }

        public void IncrementMsecPerDelSample(long value, long baseValue)
        {
            if (_pcMsecPerDelAvg != null)
            {
                lock (_pcMsecPerDelAvg)
                {
                    _pcMsecPerDelAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerDelBase.IncrementBy(baseValue);
                }
            }
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
                    _pcMsecPerDelAvg.IncrementBy(_usMsecPerDel.Current * 1000000 / Stopwatch.Frequency); //ts.Milliseconds);
                    _pcMsecPerDelBase.Increment();
                }
            }
        }


        //Bulk Counters Incrementors
        public void IncrementMsecPerAddBulkSample(long value)
        {
            if (_pcMsecPerAddBulkAvg != null)
            {
                lock (_pcMsecPerAddBulkAvg)
                {
                    _pcMsecPerAddBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerAddBulkBase.Increment();
                }
            }
        }

        public void IncrementMsecPerGetBulkSample(long value)
        {
            if (_pcMsecPerGetBulkAvg != null)
            {
                lock (_pcMsecPerGetBulkAvg)
                {
                    _pcMsecPerGetBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerGetBulkBase.Increment();
                }
            }
        }

        public void IncrementMsecPerUpdBulkSample(long value)
        {
            if (_pcMsecPerUpdBulkAvg != null)
            {
                lock (_pcMsecPerUpdBulkAvg)
                {
                    _pcMsecPerUpdBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerUpdBulkBase.Increment();
                }
            }
        }

        public void IncrementMsecPerDelBulkSample(long value)
        {
            if (_pcMsecPerDelBulkAvg != null)
            {
                lock (_pcMsecPerDelBulkAvg)
                {
                    _pcMsecPerDelBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerDelBulkBase.Increment();
                }
            }
        }


        /// <summary> 
        /// Increment the performance counter for Mirror Queue size by one. 
        /// </summary>
        public void IncrementRequestQueueSizeStats()
        {
            if (_pcReqrQueueSize != null)
            {
                lock (_pcReqrQueueSize)
                {
                    _pcReqrQueueSize.Increment();
                }
            }
        }

        /// <summary> 
        /// Decrement the performance counter for Mirror Queue size by one. 
        /// </summary>
        public void DecrementRequestQueueSizeStats()
        {
            if (_pcReqrQueueSize != null)
            {
                lock (_pcReqrQueueSize)
                {
                    if (_pcReqrQueueSize.Value > 0)
                        _pcReqrQueueSize.Decrement();
                }
            }
        }

        public void IncrementMsecPerMessagePublish(long value)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(CounterNames.AvgPublishMessage))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[CounterNames.AvgPublishMessage];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                        IncrementMessageCounter(CounterNames.AvgPublishMessageBase);
                    }
                }
            }
        }

        public void IncrementMessageCounter(string counterName)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(counterName))
            {
                PerformanceCounterBase performanceCounter = _pubsubCounterList[counterName];
                if (performanceCounter != null)
                {
                    performanceCounter.Increment();
                }
            }
        }

        public void IncrementMessagePublishedPerSec()
        {
            IncrementMessageCounter(CounterNames.MessagePublishPerSec);
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

        internal int GettingCacheServerIntervalDelay
        {
            get
            {
                return 30000;//30Secs
            }
        }

        public void StartPublishingCounters(string bindIp)
        {
            if (!_publish)
            {
                if (string.IsNullOrEmpty(bindIp))
                    bindIp = ServiceConfiguration.BindToIP.ToString();
                NCache.ServerName = bindIp; 
                if (String.IsNullOrEmpty(NCache.ServerName))
                    return;
                NCache.Port = NCache.UseTcp ? CacheConfigManager.NCacheTcpPort : CacheConfigManager.HttpPort;
            }
            _publish = true;
            _publishingThread = new Thread(PublishCounters);
            _publishingThread.IsBackground = true;
            _publishingThread.Name = "CountersPublishingThread";
            _publishingThread.Start();
        }

        internal void PublishCounters()
        {
            try
            {
                long currentVersion = -1;
                bool error = false;
                TimeSpan normalPublishingInterval = TimeSpan.FromSeconds(_publishingInterval);
                TimeSpan errorRecoveryInterval = TimeSpan.FromSeconds(5);
                TimeSpan sleepInterval;
                while (_publish)
                {
                    try
                    {
                        if (_cacheServer == null)
                            _cacheServer = NCache.GetCacheServer(new TimeSpan(0, 0, 0, 30));

                        if (!error)
                            sleepInterval = normalPublishingInterval;
                        else
                            sleepInterval = errorRecoveryInterval;

                        Thread.Sleep(sleepInterval);

                        PublishCountersToNCacheService();
                        error = false;
                    }
                    catch (ThreadAbortException)
                    {
                        break;
                    }
                    catch (ThreadInterruptedException)
                    {
                        break;
                    }
                    catch (Exception e)
                    {
                        if (_cacheServer == null)
                        {
                            Thread.Sleep(GettingCacheServerIntervalDelay);
                            continue;
                        }
                        error = true;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        internal void PublishCountersToNCacheService()
        {
            ClientCustomCounters counters = new ClientCustomCounters();
            if (_pcClientRequestsPerSec != null)
                counters._pcClientRequestsPerSec = _pcClientRequestsPerSec.Value;
            if (_pcClientResponsesPerSec != null)
                counters._pcClientResponsesPerSec = _pcClientResponsesPerSec.Value;
            if (_pcTotalClientRequestsPerSec != null)
                counters._pcTotalClientRequestsPerSec = _pcTotalClientRequestsPerSec.Value;
            if (_pcTotalClientResponsesPerSec != null)
                counters._pcTotalClientResponsesPerSec = _pcTotalClientResponsesPerSec.Value;
            if (_pcAddPerSec != null)
                counters._pcAddPerSec = _pcAddPerSec.Value;
            if (_pcGetPerSec != null)
                counters._pcGetPerSec = _pcGetPerSec.Value;
            if (_pcGetPerSec != null)
                counters._pcUpdPerSec = _pcUpdPerSec.Value;
            if (_pcDelPerSec != null)
                counters._pcDelPerSec = _pcDelPerSec.Value;
            if (_pcEventTriggeredPerSec != null)
                counters._pcEventTriggeredPerSec = _pcEventTriggeredPerSec.Value;
            if (_pcEventProcesedPerSec != null)
                counters._pcEventProcesedPerSec = _pcEventProcesedPerSec.Value;
            if (_pcReadOperationsPerSec != null)
                counters._pcReadOperationsPerSec = _pcReadOperationsPerSec.Value;
            if (_pcWriteOperationsPerSec != null)
                counters._pcWriteOperationsPerSec = _pcWriteOperationsPerSec.Value;
            if (_pcMsecPerAddBulkAvg != null)
            {
                counters._pcMsecPerAddBulkAvg = _pcMsecPerAddBulkAvg.Sum;
                counters._pcMsecPerAddBulkBase = _pcMsecPerAddBulkAvg.Total;
            }
            if (_pcMsecPerGetBulkAvg != null)
            {
                counters._pcMsecPerGetBulkAvg = _pcMsecPerGetBulkAvg.Sum;
                counters._pcMsecPerGetBulkBase = _pcMsecPerGetBulkAvg.Total;
            } 
            if (_pcMsecPerUpdBulkAvg != null)
            {
                counters._pcMsecPerUpdBulkAvg = _pcMsecPerUpdBulkAvg.Sum;
                counters._pcMsecPerUpdBulkBase = _pcMsecPerUpdBulkAvg.Total;
            }
            if (_pcMsecPerDelBulkAvg != null)
            {
                counters._pcMsecPerDelBulkAvg = _pcMsecPerDelBulkAvg.Sum;
                counters._pcMsecPerDelBulkBase = _pcMsecPerDelBulkAvg.Total;
            }
            if (_pcMsecPerGetAvg != null)
            {
                counters._pcMsecPerGetAvg = _pcMsecPerGetAvg.Sum;
                counters._pcMsecPerGetBase = _pcMsecPerGetAvg.Total;
            }
            if (_pcMsecPerAddAvg != null)
            {
                counters._pcMsecPerAddAvg = _pcMsecPerAddAvg.Sum;
                counters._pcMsecPerAddBase = _pcMsecPerAddAvg.Total;
            }
            if (_pcMsecPerUpdAvg != null)
            {
                counters._pcMsecPerUpdAvg = _pcMsecPerUpdAvg.Sum;
                counters._pcMsecPerUpdBase = _pcMsecPerUpdAvg.Total;
            }
            if (_pcMsecPerDelAvg != null)
            {
                counters._pcMsecPerDelAvg = _pcMsecPerDelAvg.Sum;
                counters._pcMsecPerDelBase = _pcMsecPerDelAvg.Total;
            }
                
            if (_pcReqrQueueSize != null)
                counters._pcReqrQueueSize = _pcReqrQueueSize.Value;
            if (_pcAvgItemSize != null)
            {
                counters._pcAvgItemSize = _pcAvgItemSize.Sum;
                counters._pcAvgItemSizeBase = _pcAvgItemSize.Total;
            }
            if (_pcMsecPerEventAvg != null)
            {
                counters._pcMsecPerEventAvg = _pcMsecPerEventAvg.Total;
                counters._pcMsecPerEventBase = _pcMsecPerEventAvg.Total;
            }    
            if (_pcMsecPerSerializationAvg != null)
            {
                counters._pcMsecPerSerializationAvg = _pcMsecPerSerializationAvg.Sum;
                counters._pcMsecPerSerializationAvgBase = _pcMsecPerSerializationAvg.Total;
            }     
            if (_pcMsecPerDeserializationAvg != null)
            {
                counters._pcMsecPerDeserializationAvg = _pcMsecPerDeserializationAvg.Sum;
                counters._pcMsecPerDeserializationAvgBase = _pcMsecPerDeserializationAvg.Total;
            }
            if (_pcMsecPerMessagePublishAvg != null)
            {
                counters._pcMsecPerMessagePublishAvg = _pcMsecPerMessagePublishAvg.Sum;
                counters._pcMsecPerMessagePublishBase = _pcMsecPerMessagePublishAvg.Total;
            }
            if (_pcMessagePublishPerSec != null)
                counters._pcMessagePublishPerSec = _pcMessagePublishPerSec.Value;
            if (_pcMessageDeliverPerSec != null)
                counters._pcMessageDeliverPerSec = _pcMessageDeliverPerSec.Value;
            
            _cacheServer.PublishCustomClientCounters(_cacheID, counters);

        }

        public string GetInstanceName(string instanceName, int port, bool inProc)
        {
            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
        }

        public void UpdateCounters(ClientCustomCounters counters)
        {
            IncrementClientRequestsPerSecStats((long)counters._pcClientRequestsPerSec);
            IncrementClientResponsesPerSecStats((long)counters._pcClientResponsesPerSec);
            IncrementTotalClientRequestsPerSecStats((long)counters._pcTotalClientRequestsPerSec);
            IncrementTotalClientResponsesPerSecStats((long)counters._pcTotalClientResponsesPerSec);
            IncrementByGetPerSecStats((long)counters._pcGetPerSec);
            IncrementByAddPerSecStats((long)counters._pcAddPerSec);
            IncrementByUpdPerSecStats((long)counters._pcUpdPerSec);
            IncrementByDelPerSecStats((long)counters._pcDelPerSec);
            IncrementEventsProcessedPerSec((long)counters._pcEventProcesedPerSec);
            IncrementEventsTriggeredPerSeconds((long)counters._pcEventTriggeredPerSec);
            _pcReqrQueueSize.Value = counters._pcReqrQueueSize;
            IncrementMsecPerMessagePublish((long)counters._pcMessagePublishPerSec);
            IncrementMessageDeliverPerSec((long)counters._pcMessageDeliverPerSec);

            _pcMsecPerAddAvg?.IncrementBy(counters._pcMsecPerAddAvg, counters._pcMsecPerAddBase);
            _pcMsecPerAddBulkAvg?.IncrementBy(counters._pcMsecPerAddBulkAvg, counters._pcMsecPerAddBulkBase);
            _pcMsecPerGetBulkAvg?.IncrementBy(counters._pcMsecPerGetBulkAvg, counters._pcMsecPerGetBulkBase);
            _pcMsecPerUpdBulkAvg?.IncrementBy(counters._pcMsecPerUpdBulkAvg, counters._pcMsecPerUpdBulkBase);
            _pcMsecPerDelBulkAvg?.IncrementBy(counters._pcMsecPerDelBulkAvg, counters._pcMsecPerDelBulkBase);
            _pcMsecPerGetAvg?.IncrementBy(counters._pcMsecPerGetAvg, counters._pcMsecPerGetBase);
            _pcMsecPerUpdAvg?.IncrementBy(counters._pcMsecPerUpdAvg, counters._pcMsecPerUpdBase);
            _pcMsecPerDelAvg?.IncrementBy(counters._pcMsecPerDelAvg, counters._pcMsecPerDelBase);
            _pcAvgItemSize?.IncrementBy(counters._pcAvgItemSize, counters._pcAvgItemSizeBase);
            _pcMsecPerEventAvg?.IncrementBy(counters._pcMsecPerEventAvg, counters._pcMsecPerEventBase);
            _pcMsecPerSerializationAvg?.IncrementBy(counters._pcMsecPerSerializationAvg, counters._pcMsecPerSerializationAvgBase);
            _pcMsecPerDeserializationAvg?.IncrementBy(counters._pcMsecPerDeserializationAvg, counters._pcMsecPerDeserializationAvgBase);
            _pcMsecPerMessagePublishAvg?.IncrementBy(counters._pcMsecPerMessagePublishAvg, counters._pcMsecPerMessagePublishBase);
        }

        public void IncrementMsecPerGetBulkSample(long value, long baseValue)
        {
            if (_pcMsecPerGetBulkAvg != null)
            {
                lock (_pcMsecPerGetBulkAvg)
                {
                    _pcMsecPerGetBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerGetBulkBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementMsecPerUpdBulkSample(long value, long baseValue)
        {
            if (_pcMsecPerUpdBulkAvg != null)
            {
                lock (_pcMsecPerUpdBulkAvg)
                {
                    _pcMsecPerUpdBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerUpdBulkBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementMsecPerDelBulkSample(long value, long baseValue)
        {
            if (_pcMsecPerDelBulkAvg != null)
            {
                lock (_pcMsecPerDelBulkAvg)
                {
                    _pcMsecPerDelBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerDelBulkBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementMsecPerAddBulkSample(long value, long baseValue)
        {
            if (_pcMsecPerAddBulkAvg != null)
            {
                lock (_pcMsecPerAddBulkAvg)
                {
                    _pcMsecPerAddBulkAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerAddBulkBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementAvgItemSize(long itemSize, long baseValue)
        {
            if (_pcAvgItemSize != null)
            {
                lock (_pcAvgItemSize)
                {
                    _pcAvgItemSize.IncrementBy(itemSize);
                    _pcAvgItemSizeBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementMsecPerDeserialization(long value, long baseValue)
        {
            if (_pcMsecPerDeserializationAvg != null)
            {
                lock (_pcMsecPerDeserializationAvg)
                {
                    _pcMsecPerDeserializationAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerDeserializationAvgBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementMsecPerSerialization(long value, long baseValue)
        {
            if (_pcMsecPerSerializationAvg != null)
            {
                lock (_pcMsecPerSerializationAvg)
                {
                    _pcMsecPerSerializationAvg.IncrementBy(value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerSerializationAvgBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementAvgEventProcessingSample(long value, long baseValue)
        {
            if (_pcMsecPerEventAvg != null)
            {
                lock (_pcMsecPerEventAvg)
                {
                    long val = (value * 1000000 / Stopwatch.Frequency);
                    _pcMsecPerEventAvg.IncrementBy(val);
                    _pcMsecPerEventBase.IncrementBy(baseValue);
                }
            }
        }

        public void IncrementEventsProcessedPerSec(long value)
        {
            if (_pcEventProcesedPerSec != null)
            {
                lock (_pcEventProcesedPerSec)
                {
                    _pcEventProcesedPerSec.IncrementBy(value);
                }
            }
        }

        public void IncrementEventsTriggeredPerSeconds(long value)
        {
            if (_pcEventTriggeredPerSec != null)
            {
                lock (_pcEventTriggeredPerSec)
                {
                    _pcEventTriggeredPerSec.IncrementBy(value);
                }
            }
        }

        internal double GetCounterValue(string counterName)
        {
            double value = 0.0;
            PerformanceCounterBase counter = null;

            switch (counterName)
            {
                case CustomCounterNames.AddPerSec:
                    counter = _pcAddPerSec;
                    break;
                case CustomCounterNames.FetchPerSec:
                    counter = _pcGetPerSec;
                    break;
                case CustomCounterNames.UpdatePerSec:
                    counter = _pcUpdPerSec;
                    break;
                case CustomCounterNames.DeletePerSec:
                    counter = _pcDelPerSec;
                    break;
                case CustomCounterNames.EventsTriggeredPerSec:
                    counter = _pcEventTriggeredPerSec;
                    break;
                case CustomCounterNames.EventProcesedPerSec:
                    counter = _pcEventProcesedPerSec;
                    break;
                case CustomCounterNames.ReadOperationsPerSec:
                    counter = _pcReadOperationsPerSec;
                    break;
                case CustomCounterNames.WriteOperationsPerSec:
                    counter = _pcWriteOperationsPerSec;
                    break;
                case CustomCounterNames.MsecPerAddBulkAvg:
                    counter = _pcMsecPerAddBulkAvg;
                    break;
                case CustomCounterNames.MsecPerAddBulkBase:
                    counter = _pcMsecPerAddBulkBase;
                    break;
                case CustomCounterNames.MsecPerGetBulkAvg:
                    counter = _pcMsecPerGetBulkAvg;
                    break;
                case CustomCounterNames.MsecPerGetBulkBase:
                    counter = _pcMsecPerGetBulkBase;
                    break;
                case CustomCounterNames.MsecPerUpdBulkAvg:
                    counter = _pcMsecPerUpdBulkAvg;
                    break;
                case CustomCounterNames.MsecPerUpdBulkBase:
                    counter = _pcMsecPerUpdBulkAvg;
                    break;
                case CustomCounterNames.MsecPerDelBulkAvg:
                    counter = _pcMsecPerDelBulkAvg;
                    break;
                case CustomCounterNames.MsecPerDelBulkBase:
                    counter = _pcMsecPerDelBulkBase;
                    break;
                case CustomCounterNames.MsecPerGetAvg:
                    counter = _pcMsecPerGetAvg;
                    break;
                case CustomCounterNames.MsecPerGetBase:
                    counter = _pcMsecPerGetBase;
                    break;
                case CustomCounterNames.MsecPerAddAvg:
                    counter = _pcMsecPerAddAvg;
                    break;
                case CustomCounterNames.MsecPerAddBase:
                    counter = _pcMsecPerAddBase;
                    break;
                case CustomCounterNames.MsecPerUpdAvg:
                    counter = _pcMsecPerUpdAvg;
                    break;
                case CustomCounterNames.MsecPerUpdBase:
                    counter = _pcMsecPerUpdBase;
                    break;
                case CustomCounterNames.MsecPerDelAvg:
                    counter = _pcMsecPerDelAvg;
                    break;
                case CustomCounterNames.MsecPerDelBase:
                    counter = _pcMsecPerDelBase;
                    break;
                case CustomCounterNames.ReqrQueueSize:
                    counter = _pcReqrQueueSize;
                    break;
                case CustomCounterNames.AvgItemSize:
                    counter = _pcAvgItemSize;
                    break;
                case CustomCounterNames.AvgItemSizeBase:
                    counter = _pcAvgItemSizeBase;
                    break;
                case CustomCounterNames.MsecPerEventAvg:
                    counter = _pcMsecPerEventAvg;
                    break;
                case CustomCounterNames.MsecPerEventBase:
                    counter = _pcMsecPerEventBase;
                    break;
                case CustomCounterNames.MsecPerSerializationAvg:
                    counter = _pcMsecPerSerializationAvg;
                    break;
                case CustomCounterNames.MsecPerSerializationAvgBase:
                    counter = _pcMsecPerSerializationAvgBase;
                    break;
                case CustomCounterNames.MsecPerDeserializationAvg:
                    counter = _pcMsecPerDeserializationAvg;
                    break;
                case CustomCounterNames.MsecPerDeserializationAvgBase:
                    counter = _pcMsecPerDeserializationAvgBase;
                    break;
                case CustomCounterNames.AvgPublishMessage:
                    counter = _pcMsecPerMessagePublishAvg;
                    break;
                case CustomCounterNames.AvgPublishMessageBase:
                    counter = _pcMsecPerMessagePublishBase;
                    break;
                case CustomCounterNames.MessagePublishPerSec:
                    counter = _pcMessagePublishPerSec;
                    break;
                case CustomCounterNames.MessageDeliveryPerSec:
                    counter = _pcMessageDeliverPerSec;
                    break;
                case CustomCounterNames.ClientRequestsPerSec:
                    counter = _pcTotalClientRequestsPerSec;
                    break;
                case CustomCounterNames.ClientResponsesPerSec:
                    counter = _pcTotalClientResponsesPerSec;
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


        public void StopPublishing()
        {
            try
            {
                lock (this)
                {
                    _publish = false;
                    if (_publishingThread != null)
#if !NETCORE
                        _publishingThread.Abort();
#elif NETCORE
                        _publishingThread.Interrupt();
#endif
                    _publishingThread = null;
                }
            }
            catch (Exception)
            {
            }
        }
    }
    
}
