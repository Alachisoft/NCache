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
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common.Util;
using System;
using System.Collections.Generic;

namespace Alachisoft.NCache.SocketServer.Statistics
{
    public class CustomStatsCollector : IDisposable, StatisticsCounter
    {
        /// <summary> Instance name. </summary>
        private string _instanceName;
        /// <summary> Port number. </summary>
        private string _port;

        /// <summary> performance counter for bytes sent per second. </summary>
        private PerformanceCounterBase _pcClientBytesSentPerSec = null;
        /// <summary> performance counter for bytes received per second. </summary>
        private PerformanceCounterBase _pcClientBytesReceiedPerSec = null;
        /// <summary> performance counter for cache requests per second. </summary>
        private PerformanceCounterBase _pcRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second. </summary>
        private PerformanceCounterBase _pcResponsesPerSec = null;

        /// <summary> performance counter for avgerage per milli-second time of all cache operations. </summary>
        private PerformanceCounterBase _pcMsecPerCacheOperation = null;
        /// <summary> performance counter for Cache max. per milli-second time of all cache operations. </summary>
        private PerformanceCounterBase _pcMsecPerCacheOperationBase = null;

        private PerformanceCounterBase _pcResponseQueueCount = null;
        private PerformanceCounterBase _pcResponseQueueSize = null;

        /// <summary> usage statistics for per milli-second time of all cache operations. </summary>
        private UsageStats _usMsecPerCacheOperation = null;
        /// <summary> usage statistics for per milli-second time of all cache operations. </summary>
        private NanoSecTimeStats _nsMsecPerCacheOperation = null;

        ///<summary> stats for event queue count</summary>
        private PerformanceCounterBase _pcEventQueueCount = null;

        /// <summary> statistics for number of client request being logged on server </summary>
        private PerformanceCounterBase _requestLogCount = null;

        private PerformanceCounterBase _requestLogPerSecond = null;

        private PerformanceCounterBase _requestLogSize = null;


        //Bulk Counters
        private PerformanceCounterBase _pcMsecPerAddBulkAvg = null;
        private PerformanceCounterBase _pcMsecPerAddBulkBase = null;
        private PerformanceCounterBase _pcMsecPerGetBulkAvg = null;
        private PerformanceCounterBase _pcMsecPerGetBulkBase = null;
        private PerformanceCounterBase _pcMsecPerUpdBulkAvg = null;
        private PerformanceCounterBase _pcMsecPerUpdBulkBase = null;
        private PerformanceCounterBase _pcMsecPerDelBulkAvg = null;
        private PerformanceCounterBase _pcMsecPerDelBulkBase = null;


        /// <summary> Category name of counter performance data.</summary>
        /// 

        private PerformanceCounterBase _pcConnectedClients = null;

        private const string PC_CATEGORY = "NCache";
        private long _queueSize;



        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public CustomStatsCollector(string instanceName, int port)
        {
            _port = ":" + port.ToString();
            _instanceName = instanceName;
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
            _pcClientBytesReceiedPerSec.Reset();
            _pcClientBytesSentPerSec.Reset();
            _pcRequestsPerSec.Reset();
            _pcResponsesPerSec.Reset();
            _pcMsecPerCacheOperationBase.Reset();
            _pcMsecPerCacheOperation.Reset();
            _pcResponseQueueCount.Reset();
            _pcResponseQueueSize.Reset();
            _pcEventQueueCount.Reset();
            _requestLogCount.Reset();
            _requestLogPerSecond.Reset();
            _requestLogSize.Reset();
            //Bulk counters
            _pcMsecPerAddBulkAvg.Reset();
            _pcMsecPerAddBulkBase.Reset();
            _pcMsecPerGetBulkAvg.Reset();
            _pcMsecPerUpdBulkAvg.Reset();
            _pcMsecPerDelBulkAvg.Reset();
            _pcMsecPerGetBulkBase.Reset();
            _pcMsecPerUpdBulkBase.Reset();
            _pcMsecPerDelBulkBase.Reset();
            _pcConnectedClients.Reset();
        }


        #endregion

        #region	/                 --- Initialization ---           /

        /// <summary>
        /// Initializes the counter instances and category.
        /// </summary>
        public void InitializePerfCounters()
        {

            lock (this)
            {
                _pcRequestsPerSec = new RateOfCounter(CustomCounterNames.RequestsPerSec, _instanceName);
                _pcResponsesPerSec = new RateOfCounter(CustomCounterNames.ResponsesPerSec, _instanceName);
                _pcClientBytesSentPerSec = new RateOfCounter(CustomCounterNames.ClientBytesSentPerSec, _instanceName);
                _pcClientBytesReceiedPerSec = new RateOfCounter(CustomCounterNames.ClientBytesReceiedPerSec, _instanceName);

                _pcMsecPerCacheOperation = new AverageCounter(CustomCounterNames.MsecPerCacheOperation, _instanceName);
                _pcMsecPerCacheOperationBase = new RateOfCounter(CustomCounterNames.MsecPerCacheOperationBase, _instanceName);

                _usMsecPerCacheOperation = new UsageStats();
                _nsMsecPerCacheOperation = new NanoSecTimeStats();

                _pcResponseQueueCount = new NumberOfItemCounter(CustomCounterNames.ResponseQueueCount, _instanceName);
                _pcResponseQueueSize = new NumberOfItemCounter(CustomCounterNames.ResponseQueueSize, _instanceName);
                _pcEventQueueCount = new NumberOfItemCounter(CustomCounterNames.EventQueueCount, _instanceName);

                _requestLogPerSecond = new RateOfCounter(CustomCounterNames.RequestLogPerSecond, _instanceName);
                _requestLogSize = new NumberOfItemCounter(CustomCounterNames.RequestLogSize, _instanceName);

                _pcConnectedClients = new NumberOfItemCounter(CustomCounterNames.ConnectedClients, _instanceName);

                //Bulk CustomCounterNames
                _pcMsecPerAddBulkAvg = new RateOfCounter(CustomCounterNames.MsecPerAddBulkAvg, _instanceName);
                _pcMsecPerGetBulkAvg = new RateOfCounter(CustomCounterNames.MsecPerGetBulkAvg, _instanceName);
                _pcMsecPerUpdBulkAvg = new RateOfCounter(CustomCounterNames.MsecPerUpdBulkAvg, _instanceName);
                _pcMsecPerDelBulkAvg = new RateOfCounter(CustomCounterNames.MsecPerDelBulkAvg, _instanceName);
                _pcMsecPerAddBulkBase = new RateOfCounter(CustomCounterNames.MsecPerAddBulkBase, _instanceName);
                _pcMsecPerGetBulkBase = new RateOfCounter(CustomCounterNames.MsecPerGetBulkBase, _instanceName);
                _pcMsecPerUpdBulkBase = new RateOfCounter(CustomCounterNames.MsecPerUpdBulkBase, _instanceName);
                _pcMsecPerDelBulkBase = new RateOfCounter(CustomCounterNames.MsecPerDelBulkBase, _instanceName);

            }
        }

        #endregion

        /// <summary> 
        /// Increment the performance counter for Client bytes sent. 
        /// </summary>
        public void IncrementBytesSentPerSecStats(long bytesSent)
        {
            if (_pcClientBytesSentPerSec != null)
            {
                lock (_pcClientBytesSentPerSec)
                {
                    _pcClientBytesSentPerSec.IncrementBy(bytesSent);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Client bytes received. 
        /// </summary>
        public void IncrementBytesReceivedPerSecStats(long bytesReceived)
        {
            if (_pcClientBytesReceiedPerSec != null)
            {
                lock (_pcClientBytesReceiedPerSec)
                {
                    _pcClientBytesReceiedPerSec.IncrementBy(bytesReceived);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Requests Per second. 
        /// </summary>
        public void IncrementRequestsPerSecStats(long requests)
        {
            if (_pcRequestsPerSec != null)
            {
                lock (_pcRequestsPerSec)
                {
                    _pcRequestsPerSec.IncrementBy(requests);
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Responses Per second. 
        /// </summary>
        public void IncrementResponsesPerSecStats(long responses)
        {
            if (_pcResponsesPerSec != null)
            {
                lock (_pcResponsesPerSec)
                {
                    _pcResponsesPerSec.IncrementBy(responses);
                }
            }
        }

        public void IncrementResponsesQueueCountStats()
        {
            if (_pcResponseQueueCount != null)
            {
                lock (_pcResponseQueueCount)
                {
                    _pcResponseQueueCount.Increment();
                }
            }
        }

        public void IncrementResponsesQueueSizeStats(long size)
        {
            if (_pcResponseQueueSize != null)
            {
                lock (_pcResponseQueueSize)
                {
                    _queueSize += size;
                    _pcResponseQueueSize.Value = _queueSize;
                }
            }
        }

        public void DecrementResponsesQueueCountStats()
        {
            if (_pcResponseQueueCount != null)
            {
                lock (_pcResponseQueueCount)
                {
                    _pcResponseQueueCount.Decrement();
                }
            }
        }

        public void DecrementResponsesQueueSizeStats(long size)
        {
            if (_pcResponseQueueSize != null)
            {
                lock (_pcResponseQueueSize)
                {
                    _queueSize -= size;
                    _pcResponseQueueSize.Value = _queueSize;
                }
            }
        }

        /// <summary>
        ///Event Queue is maintained for each client this counter shows queue count. 
        /// </summary>
        /// 
        public void IncrementEventQueueCountStats()
        {
            if (_pcEventQueueCount != null)
            {
                lock (_pcEventQueueCount)
                {
                    _pcEventQueueCount.Increment();
                }
            }
        }

        public void DecrementEventQueueCountStats()
        {
            if (_pcEventQueueCount != null)
            {
                lock (_pcEventQueueCount)
                {
                    _pcEventQueueCount.Decrement();
                }
            }
        }

        public void SetEventQueueCountStats(long count)
        {
            if (_pcEventQueueCount != null)
            {
                lock (_pcEventQueueCount)
                {
                    _pcEventQueueCount.Value = count;
                }
            }
        }


        /// <summary>
        /// Timestamps the start of sampling interval for avg. and max. per mill-second time of 
        /// all cache operations. 
        /// </summary>
        public void MsecPerCacheOperationBeginSample()
        {
            if (_pcMsecPerCacheOperation != null) lock (_nsMsecPerCacheOperation) { _nsMsecPerCacheOperation.Start(); }
        }

        /// <summary>
        /// Timestample and updates the counter for Cache avg. and max. per mill-second time of 
        /// any operation operation.
        /// </summary>
        public void MsecPerCacheOperationEndSample()
        {
            if (_pcMsecPerCacheOperation != null)
            {
                lock (_pcMsecPerCacheOperation)
                {
                    //_usMsecPerCacheOperation.EndSample();
                    _nsMsecPerCacheOperation.Stop();
                    long timeSpent = (long)_nsMsecPerCacheOperation.Duration(1);
                    if (timeSpent < 0)
                        timeSpent = timeSpent * -1;

                    _pcMsecPerCacheOperation.IncrementBy((long)timeSpent);
                    _pcMsecPerCacheOperationBase.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the Number of client requests logged on server. 
        /// </summary>
        public void IncrementRequestLogCount(long requests)
        {
            if (_requestLogCount != null)
            {
                lock (_requestLogCount)
                {
                    _requestLogCount.IncrementBy(requests);
                }
            }
        }

        /// <summary> 
        /// Decrement the Number of client requests logged on server. 
        /// </summary>
        public void DecrementRequestLogCount()
        {
            if (_requestLogCount != null)
            {
                lock (_requestLogCount)
                {
                    _requestLogCount.Decrement();
                }
            }
        }

        public void DecrementRequestLogCount(long value)
        {
            if (_requestLogCount != null)
                lock (_requestLogCount)
                {
                    _requestLogCount.Value -= value;
                }
        }

        public long RequestLogCount
        {
            set
            {
                if (_requestLogCount != null)
                    lock (_requestLogCount)
                    {
                        _requestLogCount.Value = value;
                    }
            }

        }

        public long RequestLogSize
        {
            set
            {
                if (_requestLogSize != null)
                    lock (_requestLogSize)
                        _requestLogSize.Value = value < 0 ? 0 : value;
            }
        }

        public void IncrementRequestLogPerSec()
        {
            if (_requestLogPerSecond != null)
                lock (_requestLogPerSecond)
                {
                    _requestLogPerSecond.Increment();
                }
        }



        //Bulk Counters
        public void IncrementMsecPerAddBulkAvg(long value)
        {
            if (_pcMsecPerAddBulkAvg != null)
            {
                lock (_pcMsecPerAddBulkAvg)
                {
                    _pcMsecPerAddBulkAvg.IncrementBy(value * 1000000);
                    _pcMsecPerAddBulkBase.Increment();
                }
            }
        }
        public void IncrementMsecPerGetBulkAvg(long value)
        {
            if (_pcMsecPerGetBulkAvg != null)
            {
                lock (_pcMsecPerGetBulkAvg)
                {
                    _pcMsecPerGetBulkAvg.IncrementBy(value * 1000000);
                    _pcMsecPerGetBulkBase.Increment();
                }
            }
        }

        public double GetCounterValue(string counterName)
        {
            double value = 0.0;
            PerformanceCounterBase counter = null;

            switch (counterName)
            {
                case CustomCounterNames.RequestsPerSec:
                    counter = _pcRequestsPerSec;
                    break;
                case CustomCounterNames.ResponsesPerSec:
                    counter = _pcResponsesPerSec;
                    break;
                case CustomCounterNames.ClientBytesSentPerSec:
                    counter = _pcClientBytesSentPerSec;
                    break;
                case CustomCounterNames.ClientBytesReceiedPerSec:
                    counter = _pcClientBytesReceiedPerSec;
                    break;
                case CustomCounterNames.MsecPerCacheOperation:
                    counter = _pcMsecPerCacheOperation;
                    break;
                case CustomCounterNames.MsecPerCacheOperationBase:
                    counter = _pcMsecPerCacheOperationBase;
                    break;
                case CustomCounterNames.ResponseQueueCount:
                    counter = _pcResponseQueueCount;
                    break;
                case CustomCounterNames.ResponseQueueSize:
                    counter = _pcResponseQueueSize;
                    break;
                case CustomCounterNames.EventQueueCount:
                    counter = _pcEventQueueCount;
                    break;
                case CustomCounterNames.RequestLogPerSecond:
                    counter = _requestLogPerSecond;
                    break;
                case CustomCounterNames.RequestLogSize:
                    counter = _requestLogSize;
                    break;
                case CustomCounterNames.ConnectedClients:
                    counter = _pcConnectedClients;
                    break;
                case CustomCounterNames.MsecPerAddBulkAvg:
                    counter = _pcMsecPerAddBulkAvg;
                    break;
                case CustomCounterNames.MsecPerGetBulkAvg:
                    counter = _pcMsecPerGetBulkAvg;
                    break;
                case CustomCounterNames.MsecPerUpdBulkAvg:
                    counter = _pcMsecPerUpdBulkAvg;
                    break;
                case CustomCounterNames.MsecPerDelBulkAvg:
                    counter = _pcMsecPerDelBulkAvg;
                    break;
                case CustomCounterNames.MsecPerAddBulkBase:
                    counter = _pcMsecPerAddBulkBase;
                    break;
                case CustomCounterNames.MsecPerGetBulkBase:
                    counter = _pcMsecPerGetBulkBase;
                    break;
                case CustomCounterNames.MsecPerUpdBulkBase:
                    counter = _pcMsecPerUpdBulkBase;
                    break;
                case CustomCounterNames.MsecPerDelBulkBase:
                    counter = _pcMsecPerDelBulkBase;
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


        public void IncrementMsecPerUpdBulkAvg(long value)
        {
            if (_pcMsecPerUpdBulkAvg != null)
            {
                lock (_pcMsecPerUpdBulkAvg)
                {
                    _pcMsecPerUpdBulkAvg.IncrementBy(value * 1000000);
                    _pcMsecPerUpdBulkBase.Increment();
                }
            }
        }
        public void IncrementMsecPerDelBulkAvg(long value)
        {
            if (_pcMsecPerDelBulkAvg != null)
            {
                lock (_pcMsecPerDelBulkAvg)
                {
                    _pcMsecPerDelBulkAvg.IncrementBy(value * 1000000);
                    _pcMsecPerDelBulkBase.Increment();
                }
            }
        }

        public long ConncetedClients
        {
            set
            {
                if (_pcConnectedClients != null)
                    lock (_pcConnectedClients)
                        _pcConnectedClients.Value = value < 0 ? 0 : value;
            }
            get
            {
                if (_pcConnectedClients != null)
                    lock (_pcConnectedClients)
                        return (long)_pcConnectedClients.Value;
                else
                    return ConnectionManager.ConnectionTable.Count;
            }
        }
    }
}
