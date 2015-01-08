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
using System.Management;
using System.Management.Instrumentation;
using Interlocked = System.Threading.Interlocked;

using Alachisoft.NCache.Common.Threading;
using Alachisoft.NCache.Common.Logger;

namespace Alachisoft.NCache.Common.Stats
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class SystemMemoryTask : TimeScheduler.Task, IDisposable
    {
        private const long _updateInterval = 2000;
        private const string _systemAssembly = "\\root\\cimv2";
        private string _machineName = Environment.MachineName.ToLower();

        private ManagementScope _managementScope;
        private ManagementObjectSearcher _searcher;

        private static object _mutex = new object();

        private static ulong _totalMemory;
        private static ulong _memoryUsed;

        /// <summary>
        /// 
        /// </summary>
        public SystemMemoryTask(ILogger NCacheLog)
        {

        }

        /// <summary>
        /// Get the total amount of memory
        /// </summary>
        [CLSCompliant(false)]
		public static ulong TotalMemory
        {
            get { lock (_mutex) return _totalMemory; }
        }

        /// <summary>
        /// Get the amount of memory used
        /// </summary>
        [CLSCompliant(false)]
		public static ulong MemoryUsed
        {
            get { lock (_mutex) return _memoryUsed; }
        }

        /// <summary>
        /// Percentage of memory used
        /// </summary>
        public static int PercentMemoryUsed
        {
            get { return 0; }
        }

		[CLSCompliant(false)]
        public static ulong MemoryLeft
        {
            get { lock (_mutex) return _totalMemory - _memoryUsed; }
        }

        #region Task Members

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool IsCancelled()
        {
            return false;
        }

        /// <summary>
        /// Get the next interval in which to execute Run() method
        /// </summary>
        /// <returns></returns>
        public override long GetNextInterval()
        {
            return _updateInterval;
        }

        /// <summary>
        /// Get memory status form WMI Repository
        /// </summary>
        public override void Run()
        {
            try
            {
                return;//we dont want to execute this task.
                ulong totalMemory = 0;
                ulong memoryUsed = 0;

                ManagementObjectCollection objCol = this._searcher.Get();
                foreach (ManagementObject obj in objCol)
                {
                    totalMemory = (ulong)obj.GetPropertyValue("CommitLimit") / 1024;
                    memoryUsed = (ulong)obj.GetPropertyValue("CommittedBytes") / 1024;
                }

                lock (_mutex)
                {
                    _totalMemory = totalMemory;
                    _memoryUsed = memoryUsed;
                }
            }
            catch (Exception)
            { }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            if (this._searcher != null) this._searcher.Dispose();
        }

        #endregion
    }
}
