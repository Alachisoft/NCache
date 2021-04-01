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
using Alachisoft.NCache.Common.Threading;
using System.Diagnostics;
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

            this._executionInterval = ServiceConfiguration.ForcedGCInterval;
            this._executionInterval *= 1000; ///Convert to milliseconds
            
        }

        #region Task Members

        public bool IsCancelled()
        {
            return this._cancel;
        }

        public long GetNextInterval()
        {
            return _executionInterval;
        }

        public void Run()
        {
            return;//This task will not do nothing from now ownwards
            if (_totalMemory <= 0)
            {
                _totalMemory = GetSystemMemory();
                if (_totalMemory <= 0)
                {
                    return;
                }
            }

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
                status = MemoryStatus.GetMemoryStatus();

                ///Unable to retrieve system memory status
                if (status.TotalMemory <= 0)
                {
                    _failureCount++;
                    return 0;
                }

                return status.TotalMemory;
            }
            catch (Exception exc)
            {
                _failureCount++;

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
