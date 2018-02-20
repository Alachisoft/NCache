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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;
using System.Configuration;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent "custom-policy" element in application config
    /// </summary>
    [ConfigurationRoot("cache-policy-configuration")]
    public sealed class CustomPolicyElement : ICloneable
    {

        private DatabaseType _dbType;
        /// <summary>
        /// Get or set "query" configuration element
        /// </summary>
        [ConfigurationSection("query")]
         public QueryPolicyElement[] Query { get; set; }

       

        public enum DatabaseType
        {
            /// <summary>
            /// No database dependency will be created
            /// </summary>
            None,
            /// <summary>
            /// Sql yukon and above
            /// </summary>
            Sql2005Above,
            /// <summary>
            /// Oracle 10i Release 2 and above
            /// </summary>
            Oracle10i2
        }


        [ConfigurationAttribute("database")]
        public string DBTypeString
        {
            get
            {
                return this._dbType.ToString();
            }
            set
            {
                this._dbType = DatabaseType.None;

                if (value != null)
                {
                    switch (value.ToLower())
                    {
                        case "sqlserver":
                            this._dbType = DatabaseType.Sql2005Above;
                            break;

                        case "oracle":
                            this._dbType = DatabaseType.Oracle10i2;
                            break;

                        case "none":
                        case "":
                            this._dbType = DatabaseType.None;
                            break;

                        default:
                            throw new ConfigurationErrorsException("Unknown \"database\" specified.");
                    }
                }
            }
        }

        [ConfigurationAttribute("cache-id")]
        public string CacheName { get; set; }


        #region ICloneable Members

        public object Clone()
        {
            return new CustomPolicyElement()
            {
                Query = this.Query.DeepClone() as QueryPolicyElement[]
            };

           
        }

        #endregion
    }   
}
