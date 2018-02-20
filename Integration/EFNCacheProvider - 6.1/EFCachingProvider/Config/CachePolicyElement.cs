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
using System.Configuration;

using Alachisoft.NCache.Common.Configuration;
using Alachisoft.NCache.Integrations.EntityFramework.Caching.Config;

namespace Alachisoft.NCache.Integrations.EntityFramework.Config
{
    /// <summary>
    /// Represent "cache-policy" element in application config
    /// </summary>
    public sealed class CachePolicyElement : ICloneable
    {

        /// <summary>
        /// Types of database
        /// </summary>
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

        private DatabaseType _dbType;
        private bool _cacheAllQueries = false;
        /// <summary>
        /// Default Constructor
        /// </summary>
        public CachePolicyElement()
        {
            ///Initialize to default values            
           // this.CachePolicyMode = Mode.CacheAll;
            this.Database = DatabaseType.None;
           // this.Enabled = true;
        }

        /// <summary>
        /// Get or set caching policy mode
        /// </summary>
        //public Mode CachePolicyMode
        //{
        //    get { return this._mode; }
        //    set { this._mode = value; }
        //}

        //[ConfigurationAttribute("default-policy")]
        //public string ModeString
        //{
        //    get { return this._mode.ToString(); }
        //    set
        //    {
        //        this._mode = Mode.CacheAll;
        //        if (value != null)
        //        {
        //            switch (value.ToLower())
        //            {
        //                case "cacheall":
        //                case "":
        //                    this._mode = Mode.CacheAll;
        //                    break;
        //                case "custom":
        //                    this._mode = Mode.Custom;
        //                    break;
        //                default:
        //                    throw new ConfigurationErrorsException("Unknown \"mode\" specified");
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// Get or set database type string value
        /// </summary>
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

        /// <summary>
        /// Get or set "cacheall-policy" configuration element
        /// </summary>
        //[ConfigurationSection("cacheall-policy")]
        //public CacheAllPolicyElement CacheAllPolicy { get; set; }

        /// <summary>
        /// Get or set database type
        /// </summary>
        public DatabaseType Database
        {
            get { return this._dbType; }
            set { this._dbType = value; }
        }

        /// <summary>
        /// Get or set "custom-policy" configuration element
        /// </summary>
        //[ConfigurationSection("custom-policy")]
        //public CustomPolicyElement CustomPolicy { get; set; }

        [ConfigurationSection("api-level-caching")]
        public QueryLevelCachePolicyElement APILevelCaching
        { get; set; }

        [ConfigurationSection("query")]
        public QueryPolicyElement[] Query { get; set; }


        [ConfigurationAttribute("cache-id")]
        public string CacheName { get; set; }

        [ConfigurationAttribute("cache-all-queries")]
        public bool CacheAllQueries
        {
            get
            { return _cacheAllQueries; }
            set
            {
                if (value != null)
                    _cacheAllQueries = value;
            }
        }
        /// <summary>
        /// Get or set whether caching-policy is enabled or not
        /// </summary>
        //[ConfigurationAttribute("enabled")]
        //public bool Enabled { get; set; }

        #region ICloneable Members

        public object Clone()
        {
            return new CachePolicyElement()
            {
                Database = this.Database,
               // Enabled = this.Enabled,
              //  CachePolicyMode = this.CachePolicyMode,
             //   CacheAllPolicy = this.CacheAllPolicy != null ? this.CacheAllPolicy.Clone() as CacheAllPolicyElement : null,
               // CustomPolicy = this.CustomPolicy != null ? this.CustomPolicy.Clone() as CustomPolicyElement : null,
                Query = this.Query != null ? this.Query.Clone() as QueryPolicyElement[] : null,
                CacheName = this.CacheName
            };
        }

        #endregion
    }
}
