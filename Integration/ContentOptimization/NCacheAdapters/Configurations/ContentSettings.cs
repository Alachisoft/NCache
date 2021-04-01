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

namespace Alachisoft.NCache.ContentOptimization.Configurations
{
    [Serializable]
    public class ContentSettings : ConfigurationSection
    {
        public bool Enabled
        {
            get {return EnableViewstateCaching; }
        }

        [ConfigurationProperty("webAppId")]
        public string WebApplicationId
        {
            get { return this["webAppId"]as string; }
            set { this["webAppId"] = value; }
        }


        [ConfigurationProperty("enableViewstateCaching", DefaultValue = "false", IsRequired = true)]
        public Boolean EnableViewstateCaching
        {
            get { return (Boolean)this["enableViewstateCaching"]; }
            set { this["enableViewstateCaching"] = value; }
        }

        [ConfigurationProperty("cacheSettings")]
        public CacheSettings CacheSettings
        {
            get { return this["cacheSettings"] as CacheSettings; }
            set { this["cacheSettings"] = value; }
        }

        [ConfigurationProperty("enableTrace",DefaultValue="false")]
        public Boolean EnableTrace 
        {
            get { return (Boolean)this["enableTrace"]; }
            set { this["enableTrace"] = value; }
        }

        public override bool IsReadOnly()
        {
            return false;
        }
    }
}
