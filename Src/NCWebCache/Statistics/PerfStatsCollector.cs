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
using System.Threading;
using Alachisoft.NCache.Common.Interop;


namespace Alachisoft.NCache.Web.Statistics
{
	/// <summary>
	/// Summary description for PerfStatsCollector.
	/// </summary>
	internal class PerfStatsCollector : IDisposable
	{
		/// <summary> Instance name. </summary>
		private string					_instanceName;
        /// <summary> Port number. </summary>
        private string                     _port;


        /// <summary> performance counter for cache requests per second by the client. </summary>
        private PerformanceCounter _pcClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the client. </summary>
        private PerformanceCounter _pcClientResponsesPerSec = null;
        /// <summary> performance counter for cache requests per second by all the clients. </summary>
        private PerformanceCounter _pcTotalClientRequestsPerSec = null;
        /// <summary> performance counter for cache responses per second by the all clients. </summary>
        private PerformanceCounter _pcTotalClientResponsesPerSec = null;

        private bool                    _isEnabled = false;

		/// <summary> Category name of counter performance data.</summary>
        private const string            PC_CATEGORY = "NCache";

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
                if (_pcClientRequestsPerSec != null)
                {
                    _pcClientRequestsPerSec.RemoveInstance();
                    _pcClientRequestsPerSec.Dispose();
                    _pcClientRequestsPerSec = null;
                }
                if (_pcClientResponsesPerSec != null)
                {
                    _pcClientResponsesPerSec.RemoveInstance();
                    _pcClientResponsesPerSec.Dispose();
                    _pcClientResponsesPerSec = null;
                }
                if (_pcTotalClientRequestsPerSec != null)
                {
                    _pcTotalClientRequestsPerSec.RemoveInstance();
                    _pcTotalClientRequestsPerSec.Dispose();
                    _pcTotalClientRequestsPerSec = null;
                }
                if (_pcTotalClientResponsesPerSec != null)
                {
                    _pcTotalClientResponsesPerSec.RemoveInstance();
                    _pcTotalClientResponsesPerSec.Dispose();
                    _pcTotalClientResponsesPerSec = null;
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
                    _pcClientRequestsPerSec = new PerformanceCounter(PC_CATEGORY, "Client Requests/sec", _instanceName, false);
                    _pcClientResponsesPerSec = new PerformanceCounter(PC_CATEGORY, "Client Responses/sec", _instanceName, false);
                    _pcTotalClientRequestsPerSec = new PerformanceCounter(PC_CATEGORY, "Client Requests/sec", "_Total_ client stats", false);
                    _pcTotalClientResponsesPerSec = new PerformanceCounter(PC_CATEGORY, "Client Responses/sec", "_Total_ client stats", false);
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
        /// On initialize Performance Colloection is enabled.
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
        internal void IncrementTotalClientRequestsPerSecStats(long requests)
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
        internal void IncrementTotalClientResponsesPerSecStats(long responses)
        {
            if (_pcTotalClientResponsesPerSec != null)
            {
                lock (_pcTotalClientResponsesPerSec)
                {
                    _pcTotalClientResponsesPerSec.IncrementBy(responses);
                }
            }
        }
	}
}
