// Copyright (c) 2015 Alachisoft
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

namespace Alachisoft.NCache.Web.Statistics
{
    /// <summary>
    /// Summary description for PerfStatsCollector.
    /// </summary>
    public class PerfStatsCollector2 : IDisposable
    {
        /// <summary> Instance name. </summary>
        private string _instanceName;
        /// <summary> performance counter for Cache get operations per second. </summary>
        private PerformanceCounter _pcGetPerSec = null;
        /// <summary> performance counter for Cache add operations per second. </summary>
        private PerformanceCounter _pcAddPerSec = null;
        /// <summary> performance counter for Cache update operations per second. </summary>
        private PerformanceCounter _pcUpdPerSec = null;
        /// <summary> performance counter for Cache remove operations per second. </summary>
        private PerformanceCounter _pcDelPerSec = null;
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
        private PerformanceCounter _pcMsecPerSerializationAvgBase=null;

        /// <summary> Category name of counter performance data.</summary>
        /// 
        private const string PC_CATEGORY = "NCache Client";
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        public PerfStatsCollector2(string instanceName, bool inProc)
        {
            _instanceName = GetInstanceName(instanceName, 0, inProc);

        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="instanceName"></param>
        /// <param name="port"></param>
        public PerfStatsCollector2(string instanceName, int port, bool inProc)
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
        string GetInstanceName(string instanceName, int port, bool inProc)
        {
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
              
                if (_pcReqrQueueSize != null)
                {
                    _pcReqrQueueSize.RemoveInstance();
                    _pcReqrQueueSize.Dispose();
                    _pcReqrQueueSize = null;
                }
                if (_pcAvgItemSize != null)
                {
                    _pcAvgItemSize.RemoveInstance();
                    _pcAvgItemSize.Dispose();
                    _pcAvgItemSize = null;
                    _pcAvgItemSizeBase.RemoveInstance();
                    _pcAvgItemSizeBase.Dispose();
                    _pcAvgItemSizeBase = null;
                }
                
                if (_pcEventProcesedPerSec != null)
                {
                    _pcEventProcesedPerSec.RemoveInstance();
                    _pcEventProcesedPerSec.Dispose();
                    _pcEventProcesedPerSec = null;
                }
                
                if (_pcEventTriggeredPerSec != null)
                {
                    _pcEventTriggeredPerSec.RemoveInstance();
                    _pcEventTriggeredPerSec.Dispose();
                    _pcEventTriggeredPerSec = null;
                }
                
                if (_pcMsecPerEventAvg != null)
                {
                    _pcMsecPerEventAvg.RemoveInstance();
                    _pcMsecPerEventAvg.Dispose();
                    _pcMsecPerEventAvg = null;
                }
                if (_pcMsecPerEventBase != null)
                {
                    _pcMsecPerEventBase.RemoveInstance();
                    _pcMsecPerEventBase.Dispose();
                    _pcMsecPerEventBase = null;
                }

                if (_pcMsecPerSerializationAvg != null)
                {
                    _pcMsecPerSerializationAvg.RemoveInstance();
                    _pcMsecPerSerializationAvg.Dispose();
                    _pcMsecPerSerializationAvg = null;
                }
                if (_pcMsecPerSerializationAvgBase != null)
                {
                    _pcMsecPerSerializationAvgBase.RemoveInstance();
                    _pcMsecPerSerializationAvgBase.Dispose();
                    _pcMsecPerSerializationAvgBase = null;
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

                    _pcAddPerSec = new PerformanceCounter(PC_CATEGORY, "Additions/sec", _instanceName, false);
                    _pcGetPerSec = new PerformanceCounter(PC_CATEGORY, "Fetches/sec", _instanceName, false);
                    _pcUpdPerSec = new PerformanceCounter(PC_CATEGORY, "Updates/sec", _instanceName, false);
                    _pcDelPerSec = new PerformanceCounter(PC_CATEGORY, "Deletes/sec", _instanceName, false);

                    _pcEventTriggeredPerSec = new PerformanceCounter(PC_CATEGORY, "Events Triggered/sec", _instanceName, false);
                    _pcEventProcesedPerSec = new PerformanceCounter(PC_CATEGORY, "Events Proccesed/sec", _instanceName, false);

                   
                    _pcMsecPerGetAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/fetch", _instanceName, false);
                    _pcMsecPerGetBase = new PerformanceCounter(PC_CATEGORY, "Average µs/fetch base", _instanceName, false);
                    _usMsecPerGet = new UsageStats();
                    _pcMsecPerAddAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/add", _instanceName, false);
                    _pcMsecPerAddBase = new PerformanceCounter(PC_CATEGORY, "Average µs/add base", _instanceName, false);
                    _usMsecPerAdd = new UsageStats();
                    _pcMsecPerUpdAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/insert", _instanceName, false);
                    _pcMsecPerUpdBase = new PerformanceCounter(PC_CATEGORY, "Average µs/insert base", _instanceName, false);
                    _usMsecPerUpd = new UsageStats();
                    _pcMsecPerDelAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/remove", _instanceName, false);
                    _pcMsecPerDelBase = new PerformanceCounter(PC_CATEGORY, "Average µs/remove base", _instanceName, false);
                    _usMsecPerDel = new UsageStats();

                    _pcReqrQueueSize = new PerformanceCounter(PC_CATEGORY, "Request queue size", _instanceName, false);
                    
                    _pcMsecPerEventAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/event", _instanceName, false);
                    _pcMsecPerEventBase = new PerformanceCounter(PC_CATEGORY, "Average µs/event base", _instanceName, false);
                    _usMsecPerEvent = new UsageStats();

                    _usMsecPerDel = new UsageStats();

                    _pcMsecPerSerializationAvg = new PerformanceCounter(PC_CATEGORY, "Average µs/serialization", _instanceName, false);
                    _pcMsecPerSerializationAvgBase = new PerformanceCounter(PC_CATEGORY, "Average µs/serialization base", _instanceName, false);

                }
            }
            catch (Exception e)
            {
            }

        }


        #endregion

   

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

			
        }

        /// <summary> 
        /// Increment the performance counter for Cache update operations per second. 
        /// </summary>
        public void IncrementUpdPerSecStats()
        {
            if (_pcUpdPerSec != null) lock (_pcUpdPerSec) { _pcUpdPerSec.Increment(); }

			
        }

        /// <summary> Increment the performance counter for Cache remove operations per second. </summary>
        public void IncrementDelPerSecStats()
        {
            if (_pcDelPerSec != null) lock (_pcDelPerSec) { _pcDelPerSec.Increment(); }

			
        }

        /// <summary>
        /// Timestamps the start of sampling interval for Cache avg. and max. per mill-second time of 
        /// event operations. 
        /// </summary>s
        internal void IncrementAvgEventProcessingSample(long value)
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
        internal void IncrementEventsProcessedPerSec()
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
        internal void IncrementEventsTriggeredPerSeconds()
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
            if(_pcMsecPerSerializationAvg!=null)
            {
                lock(_pcMsecPerSerializationAvg)
                {
                    _pcMsecPerSerializationAvg.IncrementBy(value * 1000000);
                    _pcMsecPerSerializationAvgBase.Increment();
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
                    _pcMsecPerGetAvg.IncrementBy(value * 1000000);
                    _pcMsecPerGetBase.Increment();
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
                    _pcMsecPerGetAvg.IncrementBy(_usMsecPerGet.Current * 1000000);
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
                    _pcMsecPerAddAvg.IncrementBy(value * 1000000);
                    _pcMsecPerAddBase.Increment();
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
                    _pcMsecPerAddAvg.IncrementBy(_usMsecPerAdd.Current * 1000000); 
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
                    _pcMsecPerUpdAvg.IncrementBy(value * 1000000); 
                    _pcMsecPerUpdBase.Increment();
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
                    _pcMsecPerUpdAvg.IncrementBy(_usMsecPerUpd.Current * 1000000);
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
                    _pcMsecPerDelAvg.IncrementBy(value * 1000000); 
                    _pcMsecPerDelBase.Increment();
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
                    _pcMsecPerDelAvg.IncrementBy(_usMsecPerDel.Current * 1000000);
                    _pcMsecPerDelBase.Increment();
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
                    if(_pcReqrQueueSize.RawValue > 0)
                        _pcReqrQueueSize.Decrement();
                }
            }
        }
    }
}
