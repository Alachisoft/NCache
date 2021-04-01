using System;
using System.Diagnostics;

using Alachisoft.NCache.Util;
using System.Threading;
using Alachisoft.NCache.Common.Interop;
using Alachisoft.NCache.Common.Stats;
using System.Collections.Generic;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Management.Statistics
{
	/// <summary>
	/// Summary description for PerfStatsCollector.
	/// </summary>
	public class PerfStatsCollector : IDisposable, StatisticsCounter
	{
		/// <summary> Instance name. </summary>
		private string					_instanceName;
        /// <summary> Port number. </summary>
        private string                     _port;

        ///// <summary> performance counter for bytes sent per second. </summary>
        ///// <summary> performance counter for bytes received per second. </summary>
        /// <summary> performance counter for cache requests per second by the client. </summary>
        private PerformanceCounter _pcClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the client. </summary>
        private PerformanceCounter _pcClientResponsesPerSec = null;
        /// <summary> performance counter for cache requests per second by all the clients. </summary>
        private PerformanceCounter _pcTotalClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the all clients. </summary>
        private PerformanceCounter _pcTotalClientResponsesPerSec = null;

        /// <summary> performance counter for Cache get operations per second. </summary>
        private PerformanceCounter _pcGetPerSec = null;
        /// <summary> performance counter for Cache add operations per second. </summary>
        private PerformanceCounter _pcAddPerSec = null;
        /// <summary> performance counter for Cache update operations per second. </summary>
        private PerformanceCounter _pcUpdPerSec = null;
        /// <summary> performance counter for Cache remove operations per second. </summary>
        private PerformanceCounter _pcDelPerSec = null;
        /// <summary> performance counter read operations per second </summary>
        private PerformanceCounter _pcReadOperationsPerSec = null;
        /// <summary> performance counter write operations per second </summary>
        private PerformanceCounter _pcWriteOperationsPerSec = null;
        /// <summary> performance counter for Mirror Queue size. </summary>
        private PerformanceCounter _pcReqrQueueSize = null;

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
        /// <summary> performance counter for Cache avg. per milli-second time of Event operations. </summary>
        private PerformanceCounter _pcMsecPerEventAvg = null;
        /// <summary> performance counter for Cache max. per milli-second time of Event operations. </summary>
        private PerformanceCounter _pcMsecPerEventBase = null;
        /// <summary> performance counter for events processed per second. </summary>
        private PerformanceCounter _pcEventProcesedPerSec = null;
        /// <summary> performance counter for events Triggered/Received per second. </summary>
        private PerformanceCounter _pcEventTriggeredPerSec = null;

        //Bulk Counters
        private PerformanceCounter _pcMsecPerAddBulkAvg = null;
        private PerformanceCounter _pcMsecPerAddBulkBase = null;

        private PerformanceCounter _pcMsecPerUpdBulkAvg = null;
        private PerformanceCounter _pcMsecPerUpdBulkBase = null;

        private PerformanceCounter _pcMsecPerGetBulkAvg = null;
        private PerformanceCounter _pcMsecPerGetBulkBase = null;

        private PerformanceCounter _pcMsecPerDelBulkAvg = null;
        private PerformanceCounter _pcMsecPerDelBulkBase = null;



        /// <summary> performance counter for</summary>
        private PerformanceCounter _pcAvgItemSize = null;
        /// <summary> base performance counter for</summary>
        private PerformanceCounter _pcAvgItemSizeBase = null;
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

        private PerformanceCounter _pcMsecPerSerializationAvg = null;
        private PerformanceCounter _pcMsecPerSerializationAvgBase = null;

        private PerformanceCounter _pcMsecPerDeserializationAvg = null;
        private PerformanceCounter _pcMsecPerDeserializationAvgBase = null;

        private PerformanceCounter _pcMsecPerMessagePublishAvg = null;
        private PerformanceCounter _pcMsecPerMessagePublishBase = null;
        private PerformanceCounter _pcMessagePublishPerSec = null;
        private PerformanceCounter _pcMessageDeliverPerSec = null;

        private Dictionary<string, PerformanceCounter> _pubsubCounterList = null;

        private bool                    _isEnabled = false;

        /// <summary> Category name of counter performance data.</summary>


        /// <summary> Category name of counter performance data.</summary>
        private const string PC_CATEGORY = "NCache Client";


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
        /// Returns true if the current user has the rights to read/write to performance counters
        /// under the category of object cache.
        /// </summary>
        public string InstanceName
		{
			get { return _instanceName; }
			set { _instanceName = value; }
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
            return !inProc ? instanceName : instanceName + " - " + Process.GetCurrentProcess().Id.ToString() + ":" + port.ToString();
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
            //we do not dispose client counters
		}


		#endregion

		#region	/                 --- Initialization ---           /

		/// <summary>
		/// Initializes the counter instances and category.
		/// </summary>
		public void InitializePerfCounters(bool inproc)
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
                    _pcClientRequestsPerSec = new PerformanceCounter(PC_CATEGORY, "Client Requests/sec", _instanceName, false);
                    _pcClientResponsesPerSec = new PerformanceCounter(PC_CATEGORY, "Client Responses/sec", _instanceName, false);
                    _pcTotalClientRequestsPerSec = new PerformanceCounter(PC_CATEGORY, "Client Requests/sec", "_Total_ client stats", false);
                    _pcTotalClientResponsesPerSec = new PerformanceCounter(PC_CATEGORY, "Client Responses/sec", "_Total_ client stats", false);

                    int ncounter = 1;
                    string instname = _instanceName;

                    _pcAddPerSec = new PerformanceCounter(PC_CATEGORY, "Additions/sec", _instanceName, false);
                    _pcGetPerSec = new PerformanceCounter(PC_CATEGORY, "Fetches/sec", _instanceName, false);
                    _pcUpdPerSec = new PerformanceCounter(PC_CATEGORY, "Updates/sec", _instanceName, false);
                    _pcDelPerSec = new PerformanceCounter(PC_CATEGORY, "Deletes/sec", _instanceName, false);

                    _pcEventTriggeredPerSec = new PerformanceCounter(PC_CATEGORY, "Events Triggered/sec", _instanceName, false);
                    _pcEventProcesedPerSec = new PerformanceCounter(PC_CATEGORY, "Events Proccesed/sec", _instanceName, false);

                    _pcReadOperationsPerSec = new PerformanceCounter(PC_CATEGORY, "Read Operations/sec", _instanceName, false);
                    _pcWriteOperationsPerSec = new PerformanceCounter(PC_CATEGORY, "Write Operations/sec", _instanceName, false);


                    //Bulk Counters
                    _pcMsecPerAddBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/addbulk", _instanceName, false);
                    _pcMsecPerAddBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/addbulk base", _instanceName, false);
                    _pcMsecPerGetBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/fetchbulk", _instanceName, false);
                    _pcMsecPerGetBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/fetchbulk base", _instanceName, false);
                    _pcMsecPerUpdBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/insertbulk", _instanceName, false);
                    _pcMsecPerUpdBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/insertbulk base", _instanceName, false);
                    _pcMsecPerDelBulkAvg = new PerformanceCounter(PC_CATEGORY, "Average us/removebulk", _instanceName, false);
                    _pcMsecPerDelBulkBase = new PerformanceCounter(PC_CATEGORY, "Average us/removebulk base", _instanceName, false);


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

                    _pcReqrQueueSize = new PerformanceCounter(PC_CATEGORY, "Request queue size", _instanceName, false);
                    _pcAvgItemSize = new PerformanceCounter(PC_CATEGORY, "Average Item Size", _instanceName, false);
                    _pcAvgItemSizeBase = new PerformanceCounter(PC_CATEGORY, "Average Item Size base", _instanceName, false);

                    _pcMsecPerEventAvg = new PerformanceCounter(PC_CATEGORY, "Average us/event", _instanceName, false);
                    _pcMsecPerEventBase = new PerformanceCounter(PC_CATEGORY, "Average us/event base", _instanceName, false);
                    _usMsecPerEvent = new UsageStats();

                    _usMsecPerDel = new UsageStats();

                    _pcMsecPerSerializationAvg = new PerformanceCounter(PC_CATEGORY, "Average us/serialization", _instanceName, false);
                    _pcMsecPerSerializationAvgBase = new PerformanceCounter(PC_CATEGORY, "Average us/serialization base", _instanceName, false);
                    _pcMsecPerDeserializationAvg = new PerformanceCounter(PC_CATEGORY, "Average us/deserialization", _instanceName, false);
                    _pcMsecPerDeserializationAvgBase = new PerformanceCounter(PC_CATEGORY, "Average us/deserialization base", _instanceName, false);



                    _pubsubCounterList = new Dictionary<string, PerformanceCounter>();
                    _pcMsecPerMessagePublishAvg = new PerformanceCounter(PC_CATEGORY, CounterNames.AvgPublishMessage, _instanceName, false);
                    _pcMsecPerMessagePublishBase = new PerformanceCounter(PC_CATEGORY, CounterNames.AvgPublishMessageBase, _instanceName, false);
                    _pcMessagePublishPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MessagePublishPerSec, _instanceName, false);
                    _pcMessageDeliverPerSec = new PerformanceCounter(PC_CATEGORY, CounterNames.MessageDeliveryPerSec, _instanceName, false);
                    _pubsubCounterList.Add(CounterNames.MessagePublishPerSec, _pcMessagePublishPerSec);
                    _pubsubCounterList.Add(CounterNames.AvgPublishMessage, _pcMsecPerMessagePublishAvg);
                    _pubsubCounterList.Add(CounterNames.AvgPublishMessageBase, _pcMsecPerMessagePublishAvg);
                    _pubsubCounterList.Add(CounterNames.MessageDeliveryPerSec, _pcMessageDeliverPerSec);

                    foreach (var item in _pubsubCounterList.Values)
                    {
                        item.RawValue = 0;
                    }
                }
                _isEnabled = true;
            }
            catch (Exception e)
            {
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
                    long val = (value * 1000000);
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
                    _pcMsecPerSerializationAvg.IncrementBy(value * 1000000);
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
                    _pcMsecPerDeserializationAvg.IncrementBy(value * 1000000);
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
                    _pcMsecPerGetAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerGetAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
        public void IncrementMsecPerAddSample(long value)
        {
            if (_pcMsecPerAddAvg != null)
            {
                lock (_pcMsecPerAddAvg)
                {
                    _pcMsecPerAddAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerAddAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerAddAvg.IncrementBy(_usMsecPerAdd.Current * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerUpdAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerUpdAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerUpdAvg.IncrementBy(_usMsecPerUpd.Current * 1000000);//ts.Milliseconds);
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
                    _pcMsecPerDelAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerDelAvg.IncrementBy(value * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerDelAvg.IncrementBy(_usMsecPerDel.Current * 1000000); //ts.Milliseconds);
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
                    _pcMsecPerAddBulkAvg.IncrementBy(value * 1000000);
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
                    _pcMsecPerGetBulkAvg.IncrementBy(value * 1000000);
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
                    _pcMsecPerUpdBulkAvg.IncrementBy(value * 1000000);
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
                    _pcMsecPerDelBulkAvg.IncrementBy(value * 1000000);
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
                    if (_pcReqrQueueSize.RawValue > 0)
                        _pcReqrQueueSize.Decrement();
                }
            }
        }

        public void IncrementMsecPerMessagePublish(long value)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(CounterNames.AvgPublishMessage))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[CounterNames.AvgPublishMessage];
                if (performanceCounter != null)
                {
                    lock (performanceCounter)
                    {
                        performanceCounter.IncrementBy(value * 1000000);
                        IncrementMessageCounter(CounterNames.AvgPublishMessageBase);
                    }
                }
            }
        }

        public void IncrementMessageCounter(string counterName)
        {
            if (_pubsubCounterList != null && _pubsubCounterList.ContainsKey(counterName))
            {
                PerformanceCounter performanceCounter = _pubsubCounterList[counterName];
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

        public void StartPublishingCounters(string bindIp)
        {
        }
	}
}
