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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;
using Alachisoft.NCache.Tools.Common;
using Alachisoft.NCache.Automation.Util;

namespace Alachisoft.NCache.Automation.ToolsParametersBase
{
    public class StartCacheParameters: ParameterBase
    {
        private string _cacheName = "";
        private string _serverName = "";
        private int _managementPort = 8250;
        static private string _partId = string.Empty;
        private static ArrayList s_cacheId = new ArrayList();

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
        public string [] Name
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

        [ArgumentAttribute(@"/s", @"/server", @"-s", @"--server")]
        [Parameter(
          Position = 1,
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.RUNNINGSERVER)]
        public string Server
        {
            get { return _serverName; }
            set
            {
                _serverName = value;

            }
        }

        [ArgumentAttribute(@"/p", @"/port", @"-p", @"--port")]
        [Parameter(
          Mandatory = false,
          ValueFromPipelineByPropertyName = true,
          ValueFromPipeline = true,
          HelpMessage = Message.PORT)]
        public int Port
        {
            get { return _managementPort; }
            set
            {
                _managementPort = value;
            }
        }

       
       
    }
}
