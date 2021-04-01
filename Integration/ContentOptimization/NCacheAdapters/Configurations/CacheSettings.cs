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
using System.Configuration;
using System.Xml;
using Alachisoft.NCache.Common.Configuration;


namespace Alachisoft.NCache.ContentOptimization.Configurations
{
    [Serializable]
    public class CacheSettings :ConfigurationElement
    {
        public CacheSettings() {  }

        [ConfigurationAttribute("cacheName")]
        [ConfigurationProperty("cacheName", IsRequired = true)]
        public string CacheName
        {
            get { return this["cacheName"] as string; }
            set { this["cacheName"] = value; }
        }

        [ConfigurationAttribute("connectionRetryInterval")]
        [ConfigurationProperty("connectionRetryInterval", IsRequired = false)]
        public int? ConnectionRetryInterval 
        {
            get 
            {
                return (int?)this["connectionRetryInterval"]; 
            }
            set { this["connectionRetryInterval"] = value; } 
        }

        [ConfigurationSection("expiration")]
        [ConfigurationProperty("expiration",IsRequired=true)]
        public Expiration Expiration 
        {
            get { return this["expiration"] as Expiration; }
            set { this["expiration"] = value; }
        }
     }    
}
