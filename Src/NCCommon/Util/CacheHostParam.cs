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
using Alachisoft.NCache.Tools.Common;

namespace Alachisoft.NCache.Common.Util
{

    public class CacheHostParam : Alachisoft.NCache.Tools.Common.CommandLineParamsBase
    {
        private string _cacheName = "";
        private string _cacheConfigPath = "";
        private int _managementPort = -1;


        public CacheHostParam()
        {

        }

        [ArgumentAttribute(@"/i", @"/cacheid", @"-i", @"--cacheid")]
        public string CacheName
        {
            get { return _cacheName; }
            set { _cacheName = value; }
        }

        [ArgumentAttribute(@"/f", @"/configfile", @"-f", @"--configfile")]
        public string CacheConfigPath
        {
            get { return _cacheConfigPath; }
            set { _cacheConfigPath = value; }
        }

        [ArgumentAttribute(@"/p", @"/managementport", @"-p", @"--managementport")]
        public int ManagementPort
        {
            get { return _managementPort; }
            set { _managementPort = value; }
        }

        [ArgumentAttribute(@"/debug", @"--debug", false)]
        public bool Debug { get; set; }

        [ArgumentAttribute(@"/startcache", @"--/startcache", false)]
        public bool StartCache { get; set; }
    }
}
