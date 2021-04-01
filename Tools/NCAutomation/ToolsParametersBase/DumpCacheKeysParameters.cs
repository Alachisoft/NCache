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
    public class DumpCacheKeysParameters :ParameterBase
    {
        private string _cacheId = "";
        private string _keyFilter = "";
        private long _keyCount = 1000;

        [Parameter(
            Position = 0,
            ValueFromPipelineByPropertyName = true,
            ValueFromPipeline = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_CACHE)]
        [ValidateNotNullOrEmpty]
        [ArgumentAttribute("", "")]
        public string Name
        {
            get { return _cacheId; }
            set { _cacheId = value; }
        }

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_KEYCOUNT)]
        [ArgumentAttribute(@"/k", @"/key-count", @"-k", @"--key-count")]
        public long KeyCount
        {
            get { return _keyCount; }
            set { _keyCount = value; }
        }

        [Parameter(
            ValueFromPipelineByPropertyName = true,
            HelpMessage = Message.DUMP_CACHE_KEYS_KEYFILTER)]
        [ArgumentAttribute(@"/F", @"/key-filter")]
        public string KeyFilter
        {
            get { return _keyFilter; }
            set { _keyFilter = value; }
        }
    }
}
