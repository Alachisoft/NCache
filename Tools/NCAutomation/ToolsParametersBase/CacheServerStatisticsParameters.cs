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
using System.Linq;
using System.Text;
using System.Collections;
using System.Management.Automation;
using Alachisoft.NCache.Automation.Util;
using Alachisoft.NCache.Tools.Common;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class CacheServerStatisticsParameters : ParameterBase
    {

        private string _server = string.Empty;
        private string _cacheId = string.Empty;
        private string _format = "tabular";
        private string _counternames = string.Empty;
        private bool doNotShowDefaultCounters = false;
        private bool continuous = false;
        private int _sampleInterval = 3;
        private long _maxSamples = 0;
        [Parameter(
         Position = 0,
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.CACHENAME)]
        [ArgumentAttribute("", "")]
        public string CacheName
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.SERVERS)]
        [ArgumentAttribute(@"/s", @"/servers", @"-s", @"--servers")]
        public string Servers
        {
            get { return _server; }
            set { _server = value; }
        }

        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.COUNTERNAMES)]
        [ArgumentAttribute(@"/c", @"/counternames", @"-c", @"--counternames")]
        public string CounterNames
        {
            get { return _counternames; }
            set { _counternames = value; }
        }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = false,
        HelpMessage = Message.DONOTSHOWDEFAULTCOUNTERS)]
        [ArgumentAttribute(@"/f", @"/detail", @"-f", @"--detail", false)]
        public SwitchParameter DoNotShowDefaultCounters { get { return doNotShowDefaultCounters; } set { doNotShowDefaultCounters = value; } }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = false,
        HelpMessage = Message.CONTINUOUS)]
        [ArgumentAttribute(@"/f", @"/detail", @"-f", @"--detail", false)]
        public SwitchParameter Continuous { get { return continuous; } set { continuous = value; } }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = true,
        HelpMessage = Message.SAMPLEINTERVAL)]
        [ArgumentAttribute(@"/c", @"/counternames", @"-c", @"--counternames")]
        public int SampleInterval
        {
            get { return _sampleInterval; }
            set { _sampleInterval = value; }
        }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = true,
        HelpMessage = Message.MAXSAMPLES)]
        [ArgumentAttribute(@"/c", @"/counternames", @"-c", @"--counternames")]
        public long MaxSamples
        {
            get { return _maxSamples; }
            set { _maxSamples = value; }
        }

        [Parameter(
        Mandatory = false,
        ValueFromPipelineByPropertyName = true,
        ValueFromPipeline = true,
        HelpMessage = Message.FORMAT)]
        [ArgumentAttribute(@"/f", @"/format", @"-f", @"--format")]
        public string Format
        {
            get { return _format; }
            set { _format = value; }
        }
    }
}
