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
    public class ClearCacheParameters : ParameterBase
    {
        private string s_cacheId = "";
        private bool s_clearJsCss = false;
        private bool s_forceClear = false;

        [Parameter(
         Position = 0,
         Mandatory = true,
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.CACHENAME)]
        [ArgumentAttribute(@"", @"")]
        public string Name
        {
            get { return s_cacheId; }
            set { s_cacheId = value; }
        }

        [Parameter(
       
         ValueFromPipelineByPropertyName = true,
         ValueFromPipeline = false,
         HelpMessage = Message.FORCECLEAR)]
        [ArgumentAttribute(@"/F",@"/forceclear", @"-F", @"--forceclear", false)]
        public SwitchParameter ForceClear
        {
            get { return s_forceClear; }
            set { s_forceClear = value; }
        }
        
    }
}
