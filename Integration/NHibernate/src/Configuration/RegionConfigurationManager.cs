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
using Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration;
using Alachisoft.NCache.Runtime.Exceptions;

namespace Alachisoft.NCache.Integrations.NHibernate.Cache.Configuration
{
    class RegionConfigurationManager
    {
        RegionConfiguration[] _regions = null;
        public RegionConfigurationManager(CacheRegions regions)
        {
            if (regions == null || regions.Regions == null)
                throw new ConfigurationException("cache-regions not specified in application config.");
            _regions = regions.Regions;
            foreach (RegionConfiguration region in _regions)
                ValidateRegionConfig(region);
        }

        public RegionConfiguration GetRegionConfig(string regionName)
        {
            for (int i = 0; i < _regions.Length; i++)
            {
                if (_regions[i].RegionName == regionName)
                {
                    return _regions[i];
                }
            }
            return null;
        }

        private void ValidateRegionConfig(RegionConfiguration region)
        {
            if (string.IsNullOrEmpty(region.RegionName))
                throw new ConfigurationException("region-name cannot be null or empty.");
            if (string.IsNullOrEmpty(region.CacheName))
                throw new ConfigurationException("cache-name cannot be null in region = " + region.RegionName);

            region.ExpirationType = region.ExpirationType.ToLower();
            if(region.ExpirationType!="absolute" && region.ExpirationType!="sliding" && region.ExpirationType!="none")
                throw new ConfigurationException("Invalid value for expiraion-type in region = " + region.RegionName);
            if (region.ExpirationType != "none")
            {
                if(region.ExpirationPeriod<=0)
                    throw new ConfigurationException("Invalid value for expiraion-period in region = " + region.RegionName + ". Expiraion period must be greater than zero.");
            }

            region.Priority = region.Priority.ToLower();
            switch (region.Priority)
            {
                case "abovenormal":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.AboveNormal;
                    break;
                case "belownormal":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.BelowNormal;
                    break;
                case "default":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.Default;
                    break;
                case "high":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.High;
                    break;
                case "low":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.Low;
                    break;
                case "normal":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.Normal;
                    break;
                case "notremovable":
                    region.CacheItemPriority = Alachisoft.NCache.Runtime.CacheItemPriority.NotRemovable;
                    break;
                default:
                    throw new ConfigurationException("Invalid value for priority in region = " + region.RegionName);
            }
        }

        public bool Contains(string regionName)
        {
            return this.GetRegionConfig(regionName) != null;
        }

        
    }

}
