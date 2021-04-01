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
    public class ListCachesParameters :ParameterBase
    {

        private string _server = string.Empty;
        private int _port = -1;
        private bool _detailed;

        [Parameter(
           ValueFromPipeline = true,
           HelpMessage = Message.SERVER)]
        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        public string Server { get { return _server; } set { _server = value; } }


        [Parameter(
        ValueFromPipelineByPropertyName  = true,
        HelpMessage = Message.PORT)]
        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        public int Port { get { return _port; } set { _port = value; } }


        [Parameter(
        ValueFromPipelineByPropertyName = true,
        HelpMessage = Message.LIST_CACHES_DETAILS)]
        [ArgumentAttribute(@"/a", @"/detail", @"-a", @"--detail", false)]
        public SwitchParameter Detail { get { return _detailed; } set { _detailed = value; } }

      
    }
}
