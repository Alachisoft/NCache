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
using Alachisoft.NCache.Runtime;
using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Common.Enum;
using Alachisoft.NCache.Common;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    class RegionConfiguration
    {
        private string _regionName;
        private string _cacheName;
        private string _priority ="Default";
       
        private string _expirationType="none";
        private int _expirationPeriod = 0;
        private CacheItemPriority _cItemPriotity;

        [ConfigurationAttribute("name")]
        public string RegionName
        {
            get { return _regionName; }
            set { _regionName = value; }
        }

        [ConfigurationAttribute("cache-name")]      
        public string CacheName
        {
            get { return _cacheName; }
            set { _cacheName = value; }
        }

        [ConfigurationAttribute("priority")]
        public string Priority
        {
            get { return _priority; }
            set { _priority = value; }
        }


        [ConfigurationAttribute("expiration-type")]
        public string ExpirationType
        {
            get { return _expirationType; }
            set { _expirationType = value; }
        }

        [ConfigurationAttribute("expiration-period")]
        public int ExpirationPeriod
        {
            get { return _expirationPeriod; }
            set { _expirationPeriod = value; }
        }

        public CacheItemPriority CacheItemPriority
        {
            get { return _cItemPriotity; }
            set { _cItemPriotity = value; }
        }

    }
}
