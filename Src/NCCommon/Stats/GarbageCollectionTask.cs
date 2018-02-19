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
using System.Collections.Generic;
using System.Text;

using Alachisoft.NCache.Common.Threading;
using System.Diagnostics;
using System.Configuration;
using Alachisoft.NCache.Common.Util;

namespace Alachisoft.NCache.Common.Stats
{
    public sealed class GarbageCollectionTask : TimeScheduler.Task
    {
        private const int _maxFilureCount = 5;
        private const int _minCollectionInterval = 5; ///5 seconds

        /// <summary>Interval after which the task will be executed agian</summary>        
        private int _executionInterval = 20000; //20 seconds

        /// <summary>Total memory available (in bytes)</summary>
        private ulong _totalMemory = 0;

        /// <summary>Usage threshold, after which GC will be invoked</summary>
        private int _collectionThreshold;

        /// <summary>Cancel the task</summary>
        private bool _cancel = false;

        /// <summary>Number of times the execution of task failed</summary>
        private short _failureCount = 0;

        public GarbageCollectionTask(int collectionThreshold)
        {            
            this._collectionThreshold = collectionThreshold;

            string intervalSettings = ConfigurationSettings.AppSettings["NCacheServer.ForcedGCInterval"];
            if (!string.IsNullOrEmpty(intervalSettings))
            {
                int.TryParse(intervalSettings, out this._executionInterval);
                if (this._executionInterval < _minCollectionInterval)
                {
                    this._executionInterval = _minCollectionInterval;
                }
                this._executionInterval *= 1000; ///Convert to milliseconds
            }
        }

        #region Task Members

        public override bool IsCancelled()
        {
            return this._cancel;
        }

        public override long GetNextInterval()
        {
            return _executionInterval;
        }

        public override void Run()
        {
            return;//This task will not do nothing from now ownwards
            if (this._totalMemory <= 0)
            {
                this._totalMemory = this.GetSystemMemory();
                if (this._totalMemory <= 0)
                {
                    return;
                }
            }

            ///[Ata]This call take some time to complete. 
            ///Benchmark @Quad core, 8GB RAM, 1,000,000 iterations
            ///Best(ms): 0.2366, Avg(ms): 0.3188, Worst(ms): 23.6123
            ulong processUsage = (ulong)Process.GetCurrentProcess().PrivateMemorySize64;

            float usage = ((float)processUsage / this._totalMemory) * 100;
            
            if (usage >= this._collectionThreshold)
            {
            }
        }

        #endregion

        /// <summary>
        /// Get system's total memory
        /// </summary>
        /// <returns>System's total memory</returns>
        private ulong GetSystemMemory()
        {
            ///Retrieve system memory information
            MemoryStatus.MemoryStatusEx status;
            try
            {
                ///This call is quiet effiecient. Also, it is called only once if we
                /// successfully retrieve system memory information (otherwise we will retry
                /// every time the task is invoked until we reach the failure count)
                ///Benchmark @Quad core, 8GB RAM, 1,000,000 iterations
                ///Best(ms): 0.003821, Avg(ms): 0.004136, Worst(ms): 0.5701
                status = MemoryStatus.GetMemoryStatus();

                ///Unable to retrieve system memory status
                if (status.TotalMemory <= 0)
                {
                    //    "Failed to retrieve memory status. Garbage collection task will not start");
                    this._failureCount++;

                    return 0;
                }

                return status.TotalMemory;
            }
            catch (Exception exc)
            {
                
                this._failureCount++;

                return 0;
            }
            finally
            {

                if (this._failureCount >= _maxFilureCount)
                {
                    AppUtil.LogEvent("NCache", "Failed to retrieve memory status. Garbage collection task will now exit",
                        EventLogEntryType.Error, EventCategories.Error, EventID.GeneralError);

                    this._cancel = true;
                }
            }
        }
    }
}
