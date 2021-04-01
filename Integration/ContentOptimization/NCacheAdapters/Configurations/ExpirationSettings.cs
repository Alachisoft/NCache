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

using System.Configuration;
using Alachisoft.NCache.Common.Configuration;

namespace Alachisoft.NCache.ContentOptimization.Configurations
{
    public enum ExpirationType
    {
        None,
        Absolute,
        Sliding
    }

    public class Expiration :ConfigurationElement
    {
        [ConfigurationAttribute("type")]
        [ConfigurationProperty("type", DefaultValue = ExpirationType.Sliding)]
        public ExpirationType ExpirationType 
        {
            get { return (ExpirationType)this["type"]; }
            set { this["type"] = value; } 
        }

        [ConfigurationAttribute("duration")]
        [ConfigurationProperty("duration", DefaultValue="5",IsRequired=true)]
        public int? Duration 
        {
            get { return (int?)this["duration"]; }
            set { this["duration"] = value; } 
        }

        public Alachisoft.ContentOptimization.Caching.Expiration Convert()
        {
            var expiration = new Alachisoft.ContentOptimization.Caching.Expiration();
            expiration.ExpirationType = (Alachisoft.ContentOptimization.Caching.ExpirationType)this.ExpirationType;
            expiration.Duration = this.Duration.HasValue ? this.Duration.Value : 5;
            return expiration;
        }

        public Alachisoft.ContentOptimization.Caching.Expiration Convert(Alachisoft.ContentOptimization.Caching.ExpirationType type)
        {
            var expiration = new Alachisoft.ContentOptimization.Caching.Expiration();
            expiration.ExpirationType = type;
            expiration.Duration = this.Duration.HasValue ? this.Duration.Value : 5;
            return expiration;
        }
    }
}
