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
using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    [ConfigurationRoot("application-config")]
    class ApplicationConfiguration
    {
        private string _applicationID;
        private bool _cacheExceptionEnabled = true;
        private string _defaultRegion;
        private bool _keyCaseSensitivity = false;
        private CacheRegions _cacheRegions;

        [ConfigurationAttribute("application-id")]
        public string ApplicationID
        {
            get { return _applicationID; }
            set { _applicationID = value; }
        }

        [ConfigurationAttribute("enable-cache-exception")]
        public bool CacheExceptionEnabled
        {
            get { return _cacheExceptionEnabled; }
            set { _cacheExceptionEnabled = value; }
        }

        [ConfigurationAttribute("default-region-name")]
        public string DefaultRegion
        {
            get { return _defaultRegion; }
            set { _defaultRegion = value; }
        }

        [ConfigurationAttribute("key-case-sensitivity")]
        public bool KeyCaseSensitivity
        {
            get { return _keyCaseSensitivity; }
            set { _keyCaseSensitivity = value; }
        }

        [ConfigurationSection("cache-regions")]
        public CacheRegions CacheRegions
        {
            get { return _cacheRegions; }
            set { _cacheRegions = value; }
        }

    }
}
