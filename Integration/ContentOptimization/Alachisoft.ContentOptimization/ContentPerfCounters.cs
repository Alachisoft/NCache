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
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using Alachisoft.ContentOptimization.Diagnostics.Counters;

namespace Alachisoft.ContentOptimization
{
    public class ContentPerfCounters : PerfCountersBase
    {
        private const string CATAGORY = "Alachisoft:ContentOptimization";
        private static ContentPerfCounters current = new ContentPerfCounters();

        private PerformanceCounter viewstateAvgSizePerSec;
        private PerformanceCounter viewstateAvgSizePerSecBase;
        private PerformanceCounter viewstateAdditionsPerSec;
        private PerformanceCounter viewstateRequestsPerSec;
        private PerformanceCounter viewstateHitsPerSec;
        private PerformanceCounter viewstateMissesPerSec;
        private PerformanceCounter resourceRequestsPerSec;

        private ContentPerfCounters()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (!UserHasAccessRights(CATAGORY))
                    return;
            }
            catch (Exception)
            {
                return;
            }            

            viewstateAvgSizePerSec = new PerformanceCounter(CATAGORY, "Viewstate size", false);
            viewstateAvgSizePerSecBase = new PerformanceCounter(CATAGORY, "Viewstate size base", false);
            viewstateAdditionsPerSec = new PerformanceCounter(CATAGORY, "Viewstate additions/sec", false);
            viewstateRequestsPerSec = new PerformanceCounter(CATAGORY, "Viewstate requests/sec", false);
            viewstateHitsPerSec = new PerformanceCounter(CATAGORY, "Viewstate hits/sec", false);
            viewstateMissesPerSec = new PerformanceCounter(CATAGORY, "Viewstate misses/sec", false);
        }
        

        public static ContentPerfCounters Current
        {
            get { return current; }
        }

        public void UpdateViewstateSize(long size)
        {
            if (viewstateAvgSizePerSec != null)
            {
                lock (viewstateAvgSizePerSec)
                {
                    viewstateAvgSizePerSec.IncrementBy(size);
                    viewstateAvgSizePerSecBase.Increment();
                }
            }           
        }

        public void IncrementViewstateCacheHits()
        {
            if (viewstateHitsPerSec != null)
                viewstateHitsPerSec.Increment(); 
        }

        public void IncrementViewstateCacheMisses()
        {
            if (viewstateMissesPerSec != null)
                viewstateMissesPerSec.Increment();
        }

        public void IncrementViewstateAdditions()
        {
            if (viewstateAdditionsPerSec != null)
                viewstateAdditionsPerSec.Increment();
        }

        public void IncrementViewstateRequests()
        {
            if (viewstateRequestsPerSec != null)
                viewstateRequestsPerSec.Increment();
        }

        public void IncrementResourceRequests()
        {
            if (resourceRequestsPerSec != null)
                resourceRequestsPerSec.Increment();
        }
    }
}
