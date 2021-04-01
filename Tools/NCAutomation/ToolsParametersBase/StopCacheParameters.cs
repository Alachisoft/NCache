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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;


namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class StopCacheParameters: ParameterBase
    {

        private string _server = "";
        private int _timeOut = 0;
        private string _cacheName = "";
        private int port = -1;
        private static ArrayList s_cacheId = new ArrayList();

        public bool _isTimeOutSet = false;

        public ArrayList CachesList
        {
            set { s_cacheId = value; }
            get { return s_cacheId; }
        }

        [Parameter(
           Position = 0,
           Mandatory = true,
           ValueFromPipelineByPropertyName = true,
           ValueFromPipeline = true,
           HelpMessage = Message.STARTCACHES)]
        [ValidateNotNullOrEmpty]
        public string[] Name
        {
            get; set;
        }

        [ArgumentAttribute(@"", @"")]
        public string CacheId
        {
            get { return _cacheName; }
            set
            {
                _cacheName = value;
                if (!String.IsNullOrEmpty(_cacheName) && !s_cacheId.Contains(_cacheName))
                    s_cacheId.Add(_cacheName);
            }
        }

        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server", "")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = false,
          HelpMessage = Message.SERVER)]
        public string Server
        {
            get { return _server; }
            set { _server = value; }
        }

        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.PORT)]
        public int Port
        {
            get { return port; }
            set { port = value; }
        }
    }
}
