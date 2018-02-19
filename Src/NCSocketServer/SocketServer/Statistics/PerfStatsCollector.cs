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

using Alachisoft.NCache.Util;
using Alachisoft.NCache.Common.Stats;
using Alachisoft.NCache.Common;


namespace Alachisoft.NCache.SocketServer.Statistics
{
	/// <summary>
	/// Summary description for PerfStatsCollector.
	/// </summary>
	public class PerfStatsCollector : IDisposable
	{
		/// <summary> Instance name. </summary>
		private string					_instanceName;
        /// <summary> Port number. </summary>
        private string                     _port;

		/// <summary> performance counter for bytes sent per second. </summary>
		private PerformanceCounter 		_pcClientBytesSentPerSec = null;
        /// <summary> performance counter for bytes received per second. </summary>
        private PerformanceCounter      _pcClientBytesReceiedPerSec = null;
        /// <summary> performance counter for cache requests per second. </summary>
        private PerformanceCounter      _pcRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second. </summary>
        private PerformanceCounter      _pcResponsesPerSec = null;

        /// <summary> performance counter for avgerage per milli-second time of all cache operations. </summary>
        private PerformanceCounter      _pcMsecPerCacheOperation = null;
        /// <summary> performance counter for Cache max. per milli-second time of all cache operations. </summary>
        private PerformanceCounter      _pcMsecPerCacheOperationBase = null;

        private PerformanceCounter _pcResponseQueueCount = null;
        private PerformanceCounter _pcResponseQueueSize = null;

        /// <summary> usage statistics for per milli-second time of all cache operations. </summary>
        private UsageStats              _usMsecPerCacheOperation = null;
        /// <summary> usage statistics for per milli-second time of all cache operations. </summary>
        private NanoSecTimeStats        _nsMsecPerCacheOperation = null;

        /// <summary> usage statistics for number of events currently in queue for cache general event notifications. </summary>
        private PerformanceCounter _generalNotificationQueueSize = null;

        ///<summary> stats for event queue count</summary>
        private PerformanceCounter _pcEventQueueCount = null;



        //Bulk Counters
        private PerformanceCounter _pcMsecPerAddBulkAvg = null;
        private PerformanceCounter _pcMsecPerAddBulkBase = null;
        private PerformanceCounter _pcMsecPerGetBulkAvg = null;
        private PerformanceCounter _pcMsecPerGetBulkBase = null;
        private PerformanceCounter _pcMsecPerUpdBulkAvg = null;
        private PerformanceCounter _pcMsecPerUpdBulkBase = null;
        private PerformanceCounter _pcMsecPerDelBulkAvg = null;
        private PerformanceCounter _pcMsecPerDelBulkBase = null;


		/// <summary> Category name of counter performance data.</summary>
        /// 

        private const string			PC_CATEGORY = "NCache";
        private long _queueSize;


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public PerfStatsCollector(string instanceName, int port)
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

                    AppUtil.LogEvent("NCache", "User has no access to NCache Server counters. " + e.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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
                if (_pcClientBytesReceiedPerSec != null)
                {
                    _pcClientBytesReceiedPerSec.RemoveInstance();
                    _pcClientBytesReceiedPerSec.Dispose();
                    _pcClientBytesReceiedPerSec = null;
                }
                if (_pcClientBytesSentPerSec != null)
                {
                    _pcClientBytesSentPerSec.RemoveInstance();
                    _pcClientBytesSentPerSec.Dispose();
                    _pcClientBytesSentPerSec = null;
                }
                if (_pcRequestsPerSec != null)
                {
                    _pcRequestsPerSec.RemoveInstance();
                    _pcRequestsPerSec.Dispose();
                    _pcRequestsPerSec = null;
                }
                if (_pcResponsesPerSec != null)
                {
                    _pcResponsesPerSec.RemoveInstance();
                    _pcResponsesPerSec.Dispose();
                    _pcResponsesPerSec = null;
                }
                if (_pcMsecPerCacheOperationBase != null)
                {
                    _pcMsecPerCacheOperationBase.RemoveInstance();
                    _pcMsecPerCacheOperationBase.Dispose();
                    _pcMsecPerCacheOperationBase = null;
                }
                if (_pcMsecPerCacheOperation != null)
                {
                    _pcMsecPerCacheOperation.RemoveInstance();
                    _pcMsecPerCacheOperation.Dispose();
                    _pcMsecPerCacheOperation = null;
                }

                if (_generalNotificationQueueSize != null)
                {
                    _generalNotificationQueueSize.RemoveInstance();
                    _generalNotificationQueueSize.Dispose();
                    _generalNotificationQueueSize = null;
                }

                if (_pcResponseQueueCount != null)
                {
                    _pcResponseQueueCount.RemoveInstance();
                    _pcResponseQueueCount.Dispose();
                    _pcResponseQueueCount = null;
                }

                if (_pcResponseQueueSize != null)
                {
                    _pcResponseQueueSize.RemoveInstance();
                    _pcResponseQueueSize.Dispose();
                    _pcResponseQueueSize = null;
                }

                if (_pcEventQueueCount != null)
                {
                    _pcEventQueueCount.RemoveInstance();
                    _pcEventQueueCount.Dispose();
                    _pcEventQueueCount = null;
                }

                //Bulk counters
                 if (_pcMsecPerAddBulkAvg != null)
                 {
                     _pcMsecPerAddBulkAvg.RemoveInstance();
                     _pcMsecPerAddBulkAvg.Dispose();
                     _pcMsecPerAddBulkAvg = null;
                 }
                 if (_pcMsecPerAddBulkBase != null)
                 {
                     _pcMsecPerAddBulkBase.RemoveInstance();
                     _pcMsecPerAddBulkBase.Dispose();
                     _pcMsecPerAddBulkBase = null;
                 }
                 if (_pcMsecPerGetBulkAvg != null)
                 {
                     _pcMsecPerGetBulkAvg.RemoveInstance();
                     _pcMsecPerGetBulkAvg.Dispose();
                     _pcMsecPerGetBulkAvg = null;
                 }
                 if (_pcMsecPerUpdBulkAvg != null)
                 {
                     _pcMsecPerUpdBulkAvg.RemoveInstance();
                     _pcMsecPerUpdBulkAvg.Dispose();
                     _pcMsecPerUpdBulkAvg = null;
                 }
                 if (_pcMsecPerDelBulkAvg != null)
                 {
                     _pcMsecPerDelBulkAvg.RemoveInstance();
                     _pcMsecPerDelBulkAvg.Dispose();
                     _pcMsecPerDelBulkAvg = null;
                 }
                 if (_pcMsecPerGetBulkBase != null)
                 {
                     _pcMsecPerGetBulkBase.RemoveInstance();
                     _pcMsecPerGetBulkBase.Dispose();
                     _pcMsecPerGetBulkBase = null;
                 }
                 if (_pcMsecPerUpdBulkBase != null)
                 {
                     _pcMsecPerUpdBulkBase.RemoveInstance();
                     _pcMsecPerUpdBulkBase.Dispose();
                     _pcMsecPerUpdBulkBase = null;
                 }
                 if (_pcMsecPerDelBulkBase != null)
                 {
                     _pcMsecPerDelBulkBase.RemoveInstance();
                     _pcMsecPerDelBulkBase.Dispose();
                     _pcMsecPerDelBulkBase = null;
                 }

            }
		}


		#endregion

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Initializes the counter instances and category.
		/// </summary>
		public void InitializePerfCounters()
		{
            try
            {
                if (!UserHasAccessRights)
                    return;

                lock (this)
                {
                    _pcRequestsPerSec = new PerformanceCounter(PC_CATEGORY, "Requests/sec", _instanceName, false);
                    _pcResponsesPerSec = new PerformanceCounter(PC_CATEGORY, "Responses/sec", _instanceName, false);
                    _pcClientBytesSentPerSec = new PerformanceCounter(PC_CATEGORY, "Client bytes sent/sec", _instanceName, false);
                    _pcClientBytesReceiedPerSec = new PerformanceCounter(PC_CATEGORY, "Client bytes received/sec", _instanceName, false);

                    _pcMsecPerCacheOperation = new PerformanceCounter(PC_CATEGORY, "Average us/cache operation", _instanceName, false);
                    _pcMsecPerCacheOperationBase = new PerformanceCounter(PC_CATEGORY, "Average us/cache operation base", _instanceName, false);

                    _usMsecPerCacheOperation = new UsageStats();
                    _nsMsecPerCacheOperation = new NanoSecTimeStats();

                    _generalNotificationQueueSize = new PerformanceCounter(PC_CATEGORY, "General Notifications Queue Size", _instanceName, false);
                    _pcResponseQueueCount = new PerformanceCounter(PC_CATEGORY, "Response Queue Count", _instanceName, false);
                    _pcResponseQueueSize = new PerformanceCounter(PC_CATEGORY, "Response Queue Size", _instanceName, false);
                    _pcEventQueueCount = new PerformanceCounter(PC_CATEGORY, "Event Queue Count", _instanceName, false);


                    //Bulk Counters
                    _pcMsecPerAddBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/addbulk", _instanceName, false);
                    _pcMsecPerGetBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/fetchbulk", _instanceName, false);
                    _pcMsecPerUpdBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/insertbulk", _instanceName, false);
                    _pcMsecPerDelBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/removebulk", _instanceName, false);
                    _pcMsecPerAddBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/addbulk base", _instanceName, false);
                    _pcMsecPerGetBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/fetchbulk base", _instanceName, false);
                    _pcMsecPerUpdBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/insertbulk base", _instanceName, false);
                    _pcMsecPerDelBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/removebulk base", _instanceName, false);

                }
            }
            catch (Exception e)
            {
                AppUtil.LogEvent("NCache", "An error occurred while initializing counters for NCache Server. " + e.ToString(), EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);
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
                    _pcResponseQueueSize.RawValue = _queueSize;
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
                    _pcResponseQueueSize.RawValue = _queueSize;
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
                    _pcEventQueueCount.RawValue = count;
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
                    _nsMsecPerCacheOperation.Stop();
                    _pcMsecPerCacheOperation.IncrementBy((long)_nsMsecPerCacheOperation.Duration(1));
                    _pcMsecPerCacheOperationBase.Increment();
                }
            }
        }

        /// <summary> 
        /// Increment the performance counter for Cache General Notification Queue. 
        /// </summary>
        public void SettNotificationQueueSizeStats(long count)
        {
            if (_generalNotificationQueueSize != null)
            {
                lock (_generalNotificationQueueSize)
                {
                    _generalNotificationQueueSize.RawValue = count;
                }
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

	}
}
