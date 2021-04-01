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
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class StressTestParameters :ParameterBase
    {
        private string _cacheId = string.Empty;
        private int _totalLoopCount = 0;
        private int _testCaseIterations = 20;
        private int _testCaseIterationDelay = 0;
        private int _getsPerIteration = 1;
        private int _updatesPerIteration = 1;
        private int _dataSize = 1024;
        private int _expiration = 60;
        private int _threadCount = 1;
        private int _reportingInterval = 5000;
        private string _server = string.Empty;

        [Parameter(
          Position = 0,
          Mandatory = true,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.CACHENAME)]
        [ValidateNotNullOrEmpty]
        [ArgumentAttribute("", "")]
        public string CacheName
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [Parameter(
     ValueFromPipelineByPropertyName = true,
     HelpMessage = Message.SERVERS)]
        [ValidateNotNullOrEmpty]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_ITEM_COUNT)]
        [ArgumentAttribute(@"/n", @"/item-count", @"-n", @"--item-count")]
        public int ItemsCount
        {
            get { return _totalLoopCount; }
            set { _totalLoopCount = value; }
        }


        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_TEST_CASE_ITERATIONS)]
        [ArgumentAttribute(@"/i", @"/test-case-iterations", @"-i", @"--test-case-iterations")]
        public int TestCaseIterations
        {
            get { return _testCaseIterations; }
            set { _testCaseIterations = value; }
        }


        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_TEST_CASE_ITERATIONS_DELAY)]
        [ArgumentAttribute(@"/d", @"/test-case-iteration-delay", @"-d", @"--test-case-iteration-delay")]
        public int TestCaseIterationDelay
        {
            get { return _testCaseIterationDelay; }
            set { _testCaseIterationDelay = value; }
        }


        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_GETS_PER_ITERATION)]
        [ArgumentAttribute(@"/g", @"/gets-per-iteration", @"-g", @"--gets-per-iteration")]
        public int GetsPerIteration
        {
            get { return _getsPerIteration; }
            set { _getsPerIteration = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_UPDATES_PER_ITERATION)]
        [ArgumentAttribute(@"/u", @"/updates-per-iteration", @"-u", @"--updates-per-iteration")]
        public int UpdatesPerIteration
        {
            get { return _updatesPerIteration; }
            set { _updatesPerIteration = value; }
        }


        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_ITEM_SIZE)]
        [ArgumentAttribute(@"/m", @"/item-size", @"-m", @"--item-size")]
        public int DataSize
        {
            get { return _dataSize; }
            set { _dataSize = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_SLIDING_EXPIRATION)]
        [ArgumentAttribute(@"/e", @"/sliding-expiration", @"-e", @"--sliding-expiration")]
        public int SlidingExpiration
        {
            get { return _expiration; }
            set { _expiration = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_THREAD_COUNT)]
        [ArgumentAttribute(@"/t", @"/thread-count", @"-t", @"--thread-count")]
        public int ThreadCount
        {
            get { return _threadCount; }
            set { _threadCount = value; }
        }

        [Parameter(
         ValueFromPipelineByPropertyName = true,
         HelpMessage = Message.STRESS_REPORT_INTERVAL)]
        [ArgumentAttribute(@"/r", @"/reporting-interval", @"-r", @"--reporting-interval")]
        public int ReportingInterval
        {
            get { return _reportingInterval; }
            set { _reportingInterval = value; }
        }
    }
}
