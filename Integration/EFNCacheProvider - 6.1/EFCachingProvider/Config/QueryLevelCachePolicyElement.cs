// Copyright (c) 2018 Alachisoft
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//    http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Alachisoft.NCache.Common.Configuration;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    public class QueryLevelCachePolicyElement : ICloneable
    {
        private Alachisoft.NCache.Integrations.EntityFramework.Config.CachePolicy.Expirations _expType;

        public QueryLevelCachePolicyElement()
        {
            _expType = CachePolicy.Expirations.Sliding;
            ExpirationTime = 180;
            DbSyncDependency = true;
            Enable = true;
        }

        public Alachisoft.NCache.Integrations.EntityFramework.Config.CachePolicy.Expirations ExpirationType
        {
            get { return this._expType; }
            set { this._expType = value; }
        }

        [ConfigurationAttribute("expiration-type")]
        public string TypeString
        {
            get { return this._expType.ToString(); }
            set
            {
                this._expType = Alachisoft.NCache.Integrations.EntityFramework.Config.CachePolicy.Expirations.Sliding;
                if (value != null)
                {
                    switch (value.ToLower())
                    {
                        case "absolute":
                        case "":
                            this._expType = EntityFramework.Config.CachePolicy.Expirations.Absolute;
                            break;
                        case "sliding":
                            this._expType = EntityFramework.Config.CachePolicy.Expirations.Sliding;
                            break;
                        default:
                            throw new ConfigurationErrorsException("Unknown \"expiration-type\" specified.");
                    }
                }
            }
        }


        [ConfigurationAttribute("enable")]
        public virtual bool Enable { get; set; }

        [ConfigurationAttribute("expiration-time", "sec")]
        public int ExpirationTime { get; set; }

        [ConfigurationAttribute("dbsyncdependency")]
        public bool DbSyncDependency { get; set; }

        public object Clone()
        {
            return new QueryLevelCachePolicyElement() {
                ExpirationTime = this.ExpirationTime,
                Enable = this.Enable,
                ExpirationType = this.ExpirationType,
                TypeString = this.TypeString,
                DbSyncDependency = this.DbSyncDependency                
            };
        }
    }
}
